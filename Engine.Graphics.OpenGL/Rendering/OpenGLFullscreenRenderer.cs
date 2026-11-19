using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLFullscreenRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly OpenGLShaderProgram _copyShader;
    private bool _disposed;
    private readonly OpenGLShaderResourceBinder _resourceBinder;
    public OpenGLFullscreenRenderer(GL gl)
    {
        ArgumentNullException.ThrowIfNull(gl);

        _gl = gl;
        _resourceBinder = new OpenGLShaderResourceBinder(gl);
        _vao = _gl.GenVertexArray();

        if (_vao == 0)
        {
            _resourceBinder.Dispose();

            throw new InvalidOperationException(
                "OpenGL failed to create a fullscreen vertex array.");
        }

        try
        {
            _copyShader = new OpenGLShaderProgram(
                gl,
                FullscreenShaderSource.Vertex,
                CopyFragmentShader);
        }
        catch
        {
            try
            {
                _resourceBinder.Dispose();
            }
            catch
            {
                // Preserve the original shader initialization error.
            }

            _gl.DeleteVertexArray(_vao);
            throw;
        }
    }

    public void DrawCopy(TextureHandle source)
    {
        Draw(_copyShader, source);
    }

    private const string CopyFragmentShader = """
    #version 330 core

    in vec2 vTexCoord;
    uniform sampler2D uSource;

    out vec4 FragColor;

    void main()
    {
    FragColor = texture(uSource, vTexCoord);
    }
    """;

    /// <summary>
    /// Draws a source texture across the currently bound framebuffer.
    /// The shader must use FullscreenShaderSource.Vertex and
    /// expose the uSource sampler uniform.
    /// </summary>
    public void Draw(
    OpenGLShaderProgram shader,
    TextureHandle source,
    IPostProcessEffect? effect = null,
    int width = 0,
    int height = 0)
    {
        Action<IShaderParameterWriter>? applyParameters =
            effect is null
                ? null
                : _ => effect.ApplyParameters(shader);

        DrawCore(
            shader,
            [
                new FullscreenPassTextureBinding(
                FullscreenShaderSource.SourceTextureUniform,
                source,
                TextureSamplerDescription.Default)
            ],
            width,
            height,
            applyParameters,
            applySamplers: false);
    }

    public void DrawWithParameters(
        OpenGLShaderProgram shader,
        TextureHandle source,
        int width,
        int height,
        Action<IShaderParameterWriter>? applyParameters)
    {
        DrawCore(
            shader,
            [
                new FullscreenPassTextureBinding(
                FullscreenShaderSource.SourceTextureUniform,
                source,
                TextureSamplerDescription.Default)
            ],
            width,
            height,
            applyParameters,
            applySamplers: false);
    }

    /// <summary>
    /// Executes a fullscreen draw with multiple named texture inputs.
    /// </summary>
    public void DrawWithResources(
        OpenGLShaderProgram shader,
        IReadOnlyList<FullscreenPassTextureBinding> inputs,
        int width,
        int height,
        Action<IShaderParameterWriter>? applyParameters)
    {
        DrawCore(
            shader,
            inputs,
            width,
            height,
            applyParameters,
            applySamplers: true);
    }

    private void DrawCore(
        OpenGLShaderProgram shader,
        IReadOnlyList<FullscreenPassTextureBinding> inputs,
        int width,
        int height,
        Action<IShaderParameterWriter>? applyParameters,
        bool applySamplers)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(shader);
        ArgumentNullException.ThrowIfNull(inputs);

        if (inputs.Count == 0)
        {
            throw new ArgumentException(
                "At least one texture input is required.",
                nameof(inputs));
        }

        if (width < 0 ||
            height < 0 ||
            (width == 0) != (height == 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Width and height must both be zero or both be positive.");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in inputs)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input.Name);

            if (!input.Texture.IsValid)
            {
                throw new ArgumentException(
                    $"Texture input '{input.Name}' is invalid.",
                    nameof(inputs));
            }

            if (!names.Add(input.Name))
            {
                throw new ArgumentException(
                    $"Texture input '{input.Name}' is duplicated.",
                    nameof(inputs));
            }
        }

        var resolutionLocation = shader.GetUniformLocation(
            FullscreenShaderSource.ResolutionUniform);

        var shaderStarted = false;
        var bindingsStarted = false;

        try
        {
            shader.Use();
            shaderStarted = true;

            if (resolutionLocation >= 0 &&
                width > 0 &&
                height > 0)
            {
                _gl.Uniform2(
                    resolutionLocation,
                    (float)width,
                    (float)height);
            }

            _resourceBinder.Begin(shader);
            bindingsStarted = true;

            foreach (var input in inputs)
            {
                _resourceBinder.SetTexture(
                    input.Name,
                    input.Texture);

                if (applySamplers)
                {
                    _resourceBinder.SetSampler(
                        input.Name,
                        input.Sampler);
                }
            }

            applyParameters?.Invoke(shader);

            _gl.BindVertexArray(_vao);

            _gl.DrawArrays(
                PrimitiveType.Triangles,
                0,
                3);
        }
        finally
        {
            try
            {
                _gl.BindVertexArray(0);
            }
            finally
            {
                try
                {
                    if (bindingsStarted)
                    {
                        _resourceBinder.End();
                    }
                }
                finally
                {
                    if (shaderStarted)
                    {
                        shader.StopUsing();
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _copyShader.Dispose();
        }
        finally
        {
            try
            {
                _resourceBinder.Dispose();
            }
            finally
            {
                _gl.DeleteVertexArray(_vao);
                _disposed = true;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}