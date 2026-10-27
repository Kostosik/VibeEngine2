using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLShaderProgram : IDisposable
{
    private readonly GL _gl;

    private uint _handle;
    private bool _disposed;

    public uint Handle
    {
        get
        {
            ThrowIfDisposed();

            return _handle;
        }
    }

    public OpenGLShaderProgram(
        GL gl,
        string vertexSource,
        string fragmentSource)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentException.ThrowIfNullOrWhiteSpace(vertexSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentSource);

        _gl = gl;

        uint vertexShader = 0;
        uint fragmentShader = 0;
        uint program = 0;

        try
        {
            vertexShader = CompileShader(
                ShaderType.VertexShader,
                vertexSource);

            fragmentShader = CompileShader(
                ShaderType.FragmentShader,
                fragmentSource);

            program = _gl.CreateProgram();

            if (program == 0)
            {
                throw new InvalidOperationException(
                    "OpenGL failed to create a shader program.");
            }

            _gl.AttachShader(program, vertexShader);
            _gl.AttachShader(program, fragmentShader);

            _gl.LinkProgram(program);

            _gl.GetProgram(
                program,
                ProgramPropertyARB.LinkStatus,
                out var linkStatus);

            if (linkStatus == 0)
            {
                var log = _gl.GetProgramInfoLog(program);

                throw new InvalidOperationException(
                    $"OpenGL shader program linking failed: {log}");
            }

            _gl.DetachShader(program, vertexShader);
            _gl.DetachShader(program, fragmentShader);

            _handle = program;
            program = 0;
        }
        finally
        {
            if (program != 0)
            {
                _gl.DeleteProgram(program);
            }

            if (vertexShader != 0)
            {
                _gl.DeleteShader(vertexShader);
            }

            if (fragmentShader != 0)
            {
                _gl.DeleteShader(fragmentShader);
            }
        }
    }

    public int GetUniformLocation(
        string name)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _gl.GetUniformLocation(
            _handle,
            name);
    }

    public void Use()
    {
        ThrowIfDisposed();

        _gl.UseProgram(
            _handle);
    }

    public void StopUsing()
    {
        ThrowIfDisposed();

        _gl.UseProgram(0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != 0)
        {
            _gl.DeleteProgram(
                _handle);

            _handle = 0;
        }

        _disposed = true;
    }

    private uint CompileShader(
        ShaderType type,
        string source)
    {
        var shader =
            _gl.CreateShader(
                type);

        _gl.ShaderSource(
            shader,
            source);

        _gl.CompileShader(
            shader);

        _gl.GetShader(
            shader,
            ShaderParameterName.CompileStatus,
            out var compileStatus);

        if (compileStatus == 0)
        {
            var log =
                _gl.GetShaderInfoLog(
                    shader);

            _gl.DeleteShader(
                shader);

            throw new InvalidOperationException(
                $"OpenGL {type} compilation failed: {log}");
        }

        return shader;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}