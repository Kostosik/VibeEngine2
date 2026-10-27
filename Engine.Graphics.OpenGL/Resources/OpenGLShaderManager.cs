using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Resources;

internal sealed class OpenGLShaderManager :
    IShaderManager,
    IDisposable
{
    private sealed class Entry
    {
        public required OpenGLShaderProgram Program
        {
            get;
            init;
        }

        public required ShaderDescription Description
        {
            get;
            init;
        }
    }

    private readonly GL _gl;

    private readonly Dictionary<
        ShaderHandle,
        Entry> _shaders =
        new();

    private uint _nextHandle = 1;

    private bool _disposed;

    public OpenGLShaderManager(
        GL gl)
    {
        ArgumentNullException.ThrowIfNull(
            gl);

        _gl = gl;
    }

    public ShaderHandle Create(
    ShaderDescription description)
    {
        ThrowIfDisposed();

        var program =
            new OpenGLShaderProgram(
                _gl,
                description.VertexSource,
                description.FragmentSource);

        try
        {
            var handle =
                CreateHandle();

            _shaders.Add(
                handle,
                new Entry
                {
                    Program = program,
                    Description = description
                });

            return handle;
        }
        catch
        {
            program.Dispose();
            throw;
        }
    }

    public bool Exists(
        ShaderHandle shader)
    {
        ThrowIfDisposed();

        return shader.IsValid &&
               _shaders.ContainsKey(
                   shader);
    }

    public ShaderDescription GetDescription(
        ShaderHandle shader)
    {
        ThrowIfDisposed();

        return GetEntry(
            shader)
            .Description;
    }

    public void Destroy(
        ShaderHandle shader)
    {
        ThrowIfDisposed();

        if (!_shaders.TryGetValue(
                shader,
                out var entry))
        {
            return;
        }

        entry.Program.Dispose();

        _shaders.Remove(
            shader);
    }

    internal OpenGLShaderProgram GetProgram(
        ShaderHandle shader)
    {
        ThrowIfDisposed();

        return GetEntry(
            shader)
            .Program;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var errors = new List<Exception>();

        foreach (var entry in _shaders.Values)
        {
            try
            {
                entry.Program.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _shaders.Clear();
        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more OpenGL shader programs could not be released.",
                errors);
        }
    }

    private ShaderHandle CreateHandle()
    {
        while (_nextHandle == 0 ||
               _shaders.ContainsKey(
                   new ShaderHandle(
                       _nextHandle)))
        {
            _nextHandle++;

            if (_nextHandle == 0)
            {
                throw new InvalidOperationException(
                    "Shader handle space is exhausted.");
            }
        }

        return new ShaderHandle(
            _nextHandle++);
    }

    private Entry GetEntry(
        ShaderHandle shader)
    {
        if (!shader.IsValid ||
            !_shaders.TryGetValue(
                shader,
                out var entry))
        {
            throw new KeyNotFoundException(
                $"Shader '{shader.Value}' was not found.");
        }

        return entry;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}