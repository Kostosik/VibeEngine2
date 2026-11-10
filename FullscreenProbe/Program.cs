using System.Diagnostics;
using Silk.NET.OpenGL;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

GlfwWindowing.Use();
double maxClearMs = 0;
double maxFinishMs = 0;
double maxSwapMs = 0;
var options = WindowOptions.Default;
options.Title = "Fullscreen Probe";
options.Size = new Vector2D<int>(1280, 720);
options.WindowState = WindowState.Normal;
options.ShouldSwapAutomatically = false;
// Повторяем настройки движка.
options.VSync = true;
options.FramesPerSecond = 60;
options.UpdatesPerSecond = 60;

using var window = Window.Create(options);

string Describe(object? value)
{
    if (value is null)
        return "<null>";

    var properties = value.GetType().GetProperties(
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Public);

    return string.Join(
        ", ",
        properties
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p =>
            {
                try
                {
                    return $"{p.Name}={p.GetValue(value)}";
                }
                catch
                {
                    return $"{p.Name}=<error>";
                }
            }));
}

void PrintDisplayInfo(string stage)
{
    Console.WriteLine(
        $"[{stage}] State={window.WindowState}, " +
        $"Size={window.Size.X}x{window.Size.Y}, " +
        $"Framebuffer={window.FramebufferSize.X}x{window.FramebufferSize.Y}, " +
        $"VSync={window.VSync}");

    Console.WriteLine(
        $"[{stage}] VideoMode: {Describe(window.VideoMode)}");

    Console.WriteLine(
        $"[{stage}] Monitor: {Describe(window.Monitor)}");
}

GL? gl = null;

var clock = Stopwatch.StartNew();

long updateFrames = 0;
long renderFrames = 0;

double nextReport = 1;
double fullscreenStarted = 0;
double normalStarted = 0;

bool fullscreenEntered = false;
bool fullscreenExited = false;
uint testFramebuffer = 0;
uint testRenderbuffer = 0;

uint testTexture = 0;
uint fullscreenVao = 0;
uint copyProgram = 0;

int sourceLocation = -1;

int targetWidth = 1920;
int targetHeight = 1080;

double maxTargetFinishMs = 0;
double maxPresentFinishMs = 0;

uint CompileShader(GL context, ShaderType type, string source)
{
    var shader = context.CreateShader(type);

    context.ShaderSource(shader, source);
    context.CompileShader(shader);

    context.GetShader(
        shader,
        ShaderParameterName.CompileStatus,
        out var status);

    if (status == 0)
    {
        var log = context.GetShaderInfoLog(shader);
        context.DeleteShader(shader);

        throw new InvalidOperationException(
            $"Shader compilation failed: {log}");
    }

    return shader;
}

uint CreateCopyProgram(GL context)
{
    const string vertexSource = """
        #version 330 core

        out vec2 vTexCoord;

        void main()
        {
            vec2 position;

            if (gl_VertexID == 0)
                position = vec2(-1.0, -1.0);
            else if (gl_VertexID == 1)
                position = vec2(3.0, -1.0);
            else
                position = vec2(-1.0, 3.0);

            vTexCoord = position * 0.5 + 0.5;
            gl_Position = vec4(position, 0.0, 1.0);
        }
        """;

    const string fragmentSource = """
        #version 330 core

        in vec2 vTexCoord;
        uniform sampler2D uSource;

        out vec4 FragColor;

        void main()
        {
            vec2 sourceUv = vec2(
                vTexCoord.x,
                1.0 - vTexCoord.y);

            FragColor = texture(uSource, sourceUv);
        }
        """;

    uint vertex = CompileShader(
        context,
        ShaderType.VertexShader,
        vertexSource);

    uint fragment = 0;
    uint program = 0;

    try
    {
        fragment = CompileShader(
            context,
            ShaderType.FragmentShader,
            fragmentSource);

        program = context.CreateProgram();

        context.AttachShader(program, vertex);
        context.AttachShader(program, fragment);
        context.LinkProgram(program);

        context.GetProgram(
            program,
            ProgramPropertyARB.LinkStatus,
            out var status);

        if (status == 0)
        {
            throw new InvalidOperationException(
                context.GetProgramInfoLog(program));
        }

        context.DetachShader(program, vertex);
        context.DetachShader(program, fragment);

        var result = program;
        program = 0;

        return result;
    }
    finally
    {
        context.DeleteShader(vertex);

        if (fragment != 0)
            context.DeleteShader(fragment);

        if (program != 0)
            context.DeleteProgram(program);
    }
}


window.Load += () =>
{
    gl = GL.GetApi(window);

    Console.WriteLine("OpenGL initialized.");
    Console.WriteLine($"GL Vendor: {gl.GetStringS(StringName.Vendor)}");
    Console.WriteLine($"GL Renderer: {gl.GetStringS(StringName.Renderer)}");
    Console.WriteLine($"GL Version: {gl.GetStringS(StringName.Version)}");

    // Создаём текстуру 1920x1080.
    testTexture = gl.GenTexture();

    gl.BindTexture(
        TextureTarget.Texture2D,
        testTexture);

    gl.TexParameter(
        TextureTarget.Texture2D,
        TextureParameterName.TextureMinFilter,
        (int)GLEnum.Linear);

    gl.TexParameter(
        TextureTarget.Texture2D,
        TextureParameterName.TextureMagFilter,
        (int)GLEnum.Linear);

    unsafe
    {
        gl.TexImage2D(
            TextureTarget.Texture2D,
            0,
            InternalFormat.Rgba8,
            (uint)targetWidth,
            (uint)targetHeight,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            null);
    }

    gl.BindTexture(TextureTarget.Texture2D, 0);

    // Создаём собственный framebuffer.
    testFramebuffer = gl.GenFramebuffer();

    gl.BindFramebuffer(
        FramebufferTarget.Framebuffer,
        testFramebuffer);

    gl.FramebufferTexture2D(
        FramebufferTarget.Framebuffer,
        FramebufferAttachment.ColorAttachment0,
        TextureTarget.Texture2D,
        testTexture,
        0);

    var status = gl.CheckFramebufferStatus(
        FramebufferTarget.Framebuffer);

    Console.WriteLine($"Test FBO status: {status}");

    gl.BindFramebuffer(
        FramebufferTarget.Framebuffer,
        0);

    if (status != GLEnum.FramebufferComplete)
    {
        throw new InvalidOperationException(
            $"Framebuffer is incomplete: {status}");
    }

    // Создаём программу копирования.
    copyProgram = CreateCopyProgram(gl);

    sourceLocation = gl.GetUniformLocation(
        copyProgram,
        "uSource");

    fullscreenVao = gl.GenVertexArray();
};

window.Render += _ =>
{
    if (gl is null)
        return;

    var size = window.FramebufferSize;

    if (size.X <= 0 || size.Y <= 0)
        return;

    long started;

    // 1. Подготавливаем сцену в собственном FBO.
    gl.BindFramebuffer(
        FramebufferTarget.Framebuffer,
        testFramebuffer);

    gl.Viewport(
        0,
        0,
        (uint)targetWidth,
        (uint)targetHeight);

    gl.ClearColor(0.1f, 0.15f, 0.25f, 1f);
    gl.Clear(ClearBufferMask.ColorBufferBit);

    started = Stopwatch.GetTimestamp();

    gl.Finish();

    maxTargetFinishMs = Math.Max(
        maxTargetFinishMs,
        Stopwatch.GetElapsedTime(started).TotalMilliseconds);

    // 2. Копируем FBO непосредственно на экран.
    gl.BindFramebuffer(
        FramebufferTarget.ReadFramebuffer,
        testFramebuffer);

    gl.BindFramebuffer(
        FramebufferTarget.DrawFramebuffer,
        0);

    started = Stopwatch.GetTimestamp();

    gl.BlitFramebuffer(
        0, 0,
        targetWidth, targetHeight,
        0, 0,
        size.X, size.Y,
        ClearBufferMask.ColorBufferBit,
        BlitFramebufferFilter.Nearest);

    gl.Finish();

    maxPresentFinishMs = Math.Max(
        maxPresentFinishMs,
        Stopwatch.GetElapsedTime(started).TotalMilliseconds);

    // Возвращаем стандартное состояние.
    gl.BindFramebuffer(
        FramebufferTarget.Framebuffer,
        0);

    // 3. Обмениваем буферы.
    started = Stopwatch.GetTimestamp();

    window.SwapBuffers();

    maxSwapMs = Math.Max(
        maxSwapMs,
        Stopwatch.GetElapsedTime(started).TotalMilliseconds);

    renderFrames++;
};

window.Resize += size =>
{
    Console.WriteLine($"Resize: {size.X}x{size.Y}");
};

window.FramebufferResize += size =>
{
    Console.WriteLine(
        $"Framebuffer: {size.X}x{size.Y}");
};

window.Update += _ =>
{
    updateFrames++;

    var elapsed = clock.Elapsed.TotalSeconds;

    if (elapsed >= nextReport)
    {
        Console.WriteLine(
            $"HEARTBEAT | Update: {updateFrames} | Render: {renderFrames} | " +
            $"FBO: {maxTargetFinishMs:F2} ms | " +
            $"Present: {maxPresentFinishMs:F2} ms | " +
            $"Swap: {maxSwapMs:F2} ms");

        maxTargetFinishMs = 0;
        maxPresentFinishMs = 0;
        maxSwapMs = 0;

        nextReport += 1;
    }

    if (!fullscreenEntered && elapsed >= 2)
    {
        Console.WriteLine("BEFORE_FULLSCREEN");
        PrintDisplayInfo("BEFORE_FULLSCREEN");
        window.WindowState = WindowState.Fullscreen;

        fullscreenEntered = true;
        fullscreenStarted = elapsed;
        PrintDisplayInfo("AFTER_FULLSCREEN");
        Console.WriteLine("AFTER_FULLSCREEN");
    }
    else if (
        fullscreenEntered &&
        !fullscreenExited &&
        elapsed - fullscreenStarted >= 5)
    {
        Console.WriteLine("BEFORE_NORMAL");

        window.WindowState = WindowState.Normal;

        fullscreenExited = true;
        normalStarted = elapsed;

        Console.WriteLine("AFTER_NORMAL");
    }
    else if (
        fullscreenExited &&
        elapsed - normalStarted >= 2)
    {
        window.Close();
    }
};

window.Initialize();
window.Run();

gl?.Dispose();

Console.WriteLine("Probe finished.");