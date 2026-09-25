using Engine.Core.Math;
using Engine.Graphics.Cameras;
using Engine.Graphics.Commands;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLTextureRenderer : IDisposable
{
    private const int SpritesPerBatch = 1024;

    private readonly GL _gl;
    private readonly Camera _camera;
    private readonly OpenGLShaderProgram _shader;

    private readonly uint _vao;
    private readonly uint _vbo;

    private readonly int _resolutionLocation;
    private readonly int _textureLocation;

    private readonly float[] _vertices =
        new float[
            SpritesPerBatch *
            6 *
            4];

    private int _vertexCount;

    private uint _currentTexture;

    private float _width;
    private float _height;

    private bool _frameActive;
    private bool _disposed;

    public OpenGLTextureRenderer(
        GL gl,
        int width,
        int height,
        Camera camera)
    {
        ArgumentNullException.ThrowIfNull(
            gl);

        ArgumentNullException.ThrowIfNull(
            camera);

        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        _gl = gl;
        _camera = camera;

        _width = width;
        _height = height;

        _shader =
            new OpenGLShaderProgram(
                _gl,
                VertexShaderSource,
                FragmentShaderSource);

        _resolutionLocation =
            _shader.GetUniformLocation(
                "uResolution");

        _textureLocation =
            _shader.GetUniformLocation(
                "uTexture");

        ValidateUniforms();

        (_vao, _vbo) =
            CreateBatchBuffer();

        Resize(
            width,
            height);
    }

    public void Begin()
    {
        ThrowIfDisposed();

        if (_frameActive)
        {
            throw new InvalidOperationException(
                "Texture renderer frame has already begun.");
        }

        _vertexCount = 0;
        _currentTexture = 0;

        _shader.Use();

        _gl.Uniform2(
            _resolutionLocation,
            _width,
            _height);

        _gl.Uniform1(
            _textureLocation,
            0);

        _gl.ActiveTexture(
            TextureUnit.Texture0);

        _gl.BindVertexArray(
            _vao);

        _frameActive = true;
    }

    public void Draw(
    DrawUiTextureCommand command)
    {
        ThrowIfDisposed();

        EnsureFrameActive();

        if (!command.Texture.IsValid)
        {
            return;
        }

        var texture =
            command.Texture.Value;

        if (_currentTexture != 0 &&
            _currentTexture != texture)
        {
            Flush();
        }

        if (_vertexCount + 6 >
            _vertices.Length / 4)
        {
            Flush();
        }

        if (_currentTexture == 0)
        {
            _currentTexture =
                texture;
        }

        if (command.ClipRect.HasValue)
        {
            AddClippedQuad(
                command.Position.X,
                command.Position.Y,
                command.Size.X,
                command.Size.Y,
                command.UV,
                command.ClipRect.Value);

            return;
        }

        AddQuad(
            command.Position.X,
            command.Position.Y,
            command.Size.X,
            command.Size.Y,
            command.UV);
    }

    private void AddClippedQuad(
    float x,
    float y,
    float width,
    float height,
    Rectangle uv,
    Rectangle clip)
    {
        if (width <= 0.0f ||
            height <= 0.0f ||
            clip.Width <= 0.0f ||
            clip.Height <= 0.0f)
        {
            return;
        }

        var left =
            MathF.Max(
                x,
                clip.X);

        var top =
            MathF.Max(
                y,
                clip.Y);

        var right =
            MathF.Min(
                x + width,
                clip.X + clip.Width);

        var bottom =
            MathF.Min(
                y + height,
                clip.Y + clip.Height);

        if (right <= left ||
            bottom <= top)
        {
            return;
        }

        var leftRatio =
            (left - x) /
            width;

        var topRatio =
            (top - y) /
            height;

        var rightRatio =
            (right - x) /
            width;

        var bottomRatio =
            (bottom - y) /
            height;

        var clippedUv =
            new Rectangle(
                uv.X +
                uv.Width *
                leftRatio,

                uv.Y +
                uv.Height *
                topRatio,

                uv.Width *
                (rightRatio -
                 leftRatio),

                uv.Height *
                (bottomRatio -
                 topRatio));

        AddQuad(
            left,
            top,
            right - left,
            bottom - top,
            clippedUv);
    }

    public void Draw(
        DrawTextureCommand command)
    {
        ThrowIfDisposed();

        EnsureFrameActive();

        if (!command.Texture.IsValid)
        {
            return;
        }

        var texture =
            command.Texture.Value;

        if (_currentTexture != 0 &&
            _currentTexture != texture)
        {
            Flush();
        }

        if (_vertexCount + 6 >
            _vertices.Length / 4)
        {
            Flush();
        }

        if (_currentTexture == 0)
        {
            _currentTexture =
                texture;
        }

        AddQuad(
            command.Position.X,
            command.Position.Y,
            command.Size.X,
            command.Size.Y,
            command.UV);
    }

    public void DrawWorld(
        DrawWorldTextureCommand command)
    {
        ThrowIfDisposed();

        EnsureFrameActive();

        if (!command.Texture.IsValid)
        {
            return;
        }

        var position =
            _camera.WorldToScreen(
                command.Position);

        var size =
            command.Size *
            _camera.Zoom;

        Draw(
            new DrawTextureCommand(
                command.Texture,
                position,
                size,
                command.UV));
    }

    public void End()
    {
        ThrowIfDisposed();

        EnsureFrameActive();

        Flush();

        _gl.BindVertexArray(
            0);

        _gl.BindTexture(
            TextureTarget.Texture2D,
            0);

        _shader.StopUsing();

        _frameActive = false;
    }

    public void Resize(
        int width,
        int height)
    {
        ThrowIfDisposed();

        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        if (_frameActive)
        {
            Flush();

            _gl.Uniform2(
                _resolutionLocation,
                width,
                height);
        }

        _width = width;
        _height = height;

        _gl.Viewport(
            0,
            0,
            (uint)width,
            (uint)height);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _shader.Dispose();

        _gl.DeleteBuffer(
            _vbo);

        _gl.DeleteVertexArray(
            _vao);

        _disposed = true;
    }

    private void AddQuad(
        float x,
        float y,
        float width,
        float height,
        Rectangle uv)
    {
        var right =
            x + width;

        var bottom =
            y + height;

        AddVertex(
            x,
            y,
            uv.X,
            uv.Y);

        AddVertex(
            right,
            y,
            uv.X + uv.Width,
            uv.Y);

        AddVertex(
            right,
            bottom,
            uv.X + uv.Width,
            uv.Y + uv.Height);

        AddVertex(
            x,
            y,
            uv.X,
            uv.Y);

        AddVertex(
            right,
            bottom,
            uv.X + uv.Width,
            uv.Y + uv.Height);

        AddVertex(
            x,
            bottom,
            uv.X,
            uv.Y + uv.Height);
    }

    private void AddVertex(
        float x,
        float y,
        float u,
        float v)
    {
        var index =
            _vertexCount * 4;

        _vertices[index] =
            x;

        _vertices[index + 1] =
            y;

        _vertices[index + 2] =
            u;

        _vertices[index + 3] =
            v;

        _vertexCount++;
    }

    private void Flush()
    {
        if (_vertexCount == 0)
        {
            return;
        }

        if (_currentTexture == 0)
        {
            return;
        }

        _gl.BindTexture(
            TextureTarget.Texture2D,
            _currentTexture);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vbo);

        var vertexData =
            _vertices.AsSpan(
                0,
                _vertexCount * 4);

        _gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            vertexData,
            BufferUsageARB.DynamicDraw);

        _gl.DrawArrays(
            PrimitiveType.Triangles,
            0,
            (uint)_vertexCount);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);

        _vertexCount = 0;
        _currentTexture = 0;
    }

    private (uint Vao, uint Vbo) CreateBatchBuffer()
    {
        var vao =
            _gl.GenVertexArray();

        var vbo =
            _gl.GenBuffer();

        _gl.BindVertexArray(
            vao);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            vbo);

        _gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            _vertices,
            BufferUsageARB.DynamicDraw);

        const uint stride =
            4 * sizeof(float);

        _gl.VertexAttribPointer(
            0,
            2,
            VertexAttribPointerType.Float,
            false,
            stride,
            0);

        _gl.EnableVertexAttribArray(
            0);

        _gl.VertexAttribPointer(
            1,
            2,
            VertexAttribPointerType.Float,
            false,
            stride,
            2 * sizeof(float));

        _gl.EnableVertexAttribArray(
            1);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);

        _gl.BindVertexArray(
            0);

        return (
            vao,
            vbo);
    }

    private void ValidateUniforms()
    {
        if (_resolutionLocation < 0)
        {
            throw new InvalidOperationException(
                "Uniform 'uResolution' was not found.");
        }

        if (_textureLocation < 0)
        {
            throw new InvalidOperationException(
                "Uniform 'uTexture' was not found.");
        }
    }

    private void EnsureFrameActive()
    {
        if (!_frameActive)
        {
            throw new InvalidOperationException(
                "Texture renderer frame has not begun.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private const string VertexShaderSource = """
        #version 330 core

        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aTexCoord;

        uniform vec2 uResolution;

        out vec2 vTexCoord;

        void main()
        {
            vec2 normalized =
                aPosition /
                uResolution;

            vec2 clipPosition =
                normalized * 2.0 - 1.0;

            clipPosition.y =
                -clipPosition.y;

            gl_Position =
                vec4(
                    clipPosition,
                    0.0,
                    1.0);

            vTexCoord =
                aTexCoord;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uTexture;

        out vec4 FragColor;

        void main()
        {
            FragColor =
                texture(
                    uTexture,
                    vTexCoord);
        }
        """;
}