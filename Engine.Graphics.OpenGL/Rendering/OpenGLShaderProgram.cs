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

        var vertexShader =
            CompileShader(
                ShaderType.VertexShader,
                vertexSource);

        var fragmentShader =
            CompileShader(
                ShaderType.FragmentShader,
                fragmentSource);

        _handle =
            _gl.CreateProgram();

        _gl.AttachShader(
            _handle,
            vertexShader);

        _gl.AttachShader(
            _handle,
            fragmentShader);

        _gl.LinkProgram(
            _handle);

        _gl.GetProgram(
            _handle,
            ProgramPropertyARB.LinkStatus,
            out var linkStatus);

        if (linkStatus == 0)
        {
            var log =
                _gl.GetProgramInfoLog(
                    _handle);

            _gl.DeleteShader(
                vertexShader);

            _gl.DeleteShader(
                fragmentShader);

            _gl.DeleteProgram(
                _handle);

            _handle = 0;

            throw new InvalidOperationException(
                $"OpenGL shader program linking failed: {log}");
        }

        _gl.DetachShader(
            _handle,
            vertexShader);

        _gl.DetachShader(
            _handle,
            fragmentShader);

        _gl.DeleteShader(
            vertexShader);

        _gl.DeleteShader(
            fragmentShader);
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