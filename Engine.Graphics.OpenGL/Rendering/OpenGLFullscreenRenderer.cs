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

    public OpenGLFullscreenRenderer(GL gl)
    {
        ArgumentNullException.ThrowIfNull(gl);

        _gl = gl;

        _vao = _gl.GenVertexArray();

        if (_vao == 0)
        {
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
        vec2 sourceUv = vec2(
            vTexCoord.x,
            1.0 - vTexCoord.y);

        FragColor = texture(uSource, sourceUv);
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
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(shader);

        if (!source.IsValid)
        {
            throw new ArgumentException(
                "Source texture handle is invalid.",
                nameof(source));
        }

        if (width < 0 ||
            height < 0 ||
            (width == 0) != (height == 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Width and height must both be zero or both be positive.");
        }

        var sourceLocation = shader.GetUniformLocation(
            FullscreenShaderSource.SourceTextureUniform);

        var resolutionLocation = shader.GetUniformLocation(
            FullscreenShaderSource.ResolutionUniform);

        if (sourceLocation < 0)
        {
            throw new InvalidOperationException(
                $"Fullscreen shader must declare " +
                $"'{FullscreenShaderSource.SourceTextureUniform}'.");
        }

        var shaderStarted = false;

        try
        {
            shader.Use();
            shaderStarted = true;

            // Uniform устанавливаем только после активации программы.
            if (resolutionLocation >= 0 &&
                width > 0 &&
                height > 0)
            {
                _gl.Uniform2(
                    resolutionLocation,
                    (float)width,
                    (float)height);
            }

            _gl.ActiveTexture(TextureUnit.Texture0);

            _gl.BindTexture(
                TextureTarget.Texture2D,
                source.Value);

            _gl.Uniform1(sourceLocation, 0);

            effect?.ApplyParameters(shader);

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
                    _gl.ActiveTexture(TextureUnit.Texture0);

                    _gl.BindTexture(
                        TextureTarget.Texture2D,
                        0);
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
            _gl.DeleteVertexArray(_vao);
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}