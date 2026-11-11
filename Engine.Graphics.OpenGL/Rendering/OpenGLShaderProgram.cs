using Engine.Graphics.Rendering;
using Engine.Core.Math;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLShaderProgram :
    IDisposable,
    IPostProcessParameterWriter
{
    private readonly GL _gl;
    private readonly Dictionary<string, int> _uniformLocations =
    new(StringComparer.Ordinal);
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

    public void SetMatrix4x4(
    string name,
    Matrix4x4 value)
    {
        if (!float.IsFinite(value.M11) ||
            !float.IsFinite(value.M12) ||
            !float.IsFinite(value.M13) ||
            !float.IsFinite(value.M14) ||
            !float.IsFinite(value.M21) ||
            !float.IsFinite(value.M22) ||
            !float.IsFinite(value.M23) ||
            !float.IsFinite(value.M24) ||
            !float.IsFinite(value.M31) ||
            !float.IsFinite(value.M32) ||
            !float.IsFinite(value.M33) ||
            !float.IsFinite(value.M34) ||
            !float.IsFinite(value.M41) ||
            !float.IsFinite(value.M42) ||
            !float.IsFinite(value.M43) ||
            !float.IsFinite(value.M44))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var location = GetUniformLocation(name);

        if (location < 0)
        {
            throw new InvalidOperationException(
                $"Shader uniform '{name}' was not found.");
        }

        // Данные располагаются построчно.
        // OpenGL интерпретирует их как столбцы, поэтому
        // получаем транспонированную матрицу для GLSL.
        Span<float> values = stackalloc float[16];

        values[0] = value.M11;
        values[1] = value.M12;
        values[2] = value.M13;
        values[3] = value.M14;

        values[4] = value.M21;
        values[5] = value.M22;
        values[6] = value.M23;
        values[7] = value.M24;

        values[8] = value.M31;
        values[9] = value.M32;
        values[10] = value.M33;
        values[11] = value.M34;

        values[12] = value.M41;
        values[13] = value.M42;
        values[14] = value.M43;
        values[15] = value.M44;

        unsafe
        {
            fixed (float* pointer = values)
            {
                _gl.UniformMatrix4(
                    location,
                    1,
                    false,
                    pointer);
            }
        }
    }

    public int GetUniformLocation(string name)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_uniformLocations.TryGetValue(name, out var location))
            return location;

        location = _gl.GetUniformLocation(_handle, name);
        _uniformLocations.Add(name, location);

        return location;
    }

    public void SetFloat(string name, float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var location = GetUniformLocation(name);

        if (location < 0)
        {
            throw new InvalidOperationException(
                $"Shader uniform '{name}' was not found.");
        }

        _gl.Uniform1(location, value);
    }

    public void SetVector3(string name, Vector3 value)
    {
        if (!float.IsFinite(value.X) ||
            !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Z))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var location = GetUniformLocation(name);

        if (location < 0)
        {
            throw new InvalidOperationException(
                $"Shader uniform '{name}' was not found.");
        }

        _gl.Uniform3(
            location,
            value.X,
            value.Y,
            value.Z);
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