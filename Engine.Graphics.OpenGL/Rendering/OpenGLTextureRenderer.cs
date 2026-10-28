using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics2D.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLTextureRenderer : IDisposable
{
    private const int SpritesPerBatch = 1024;

    private readonly GL _gl;
    private readonly Camera2D _camera;
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
        Camera2D camera)
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

        try
        {
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
        catch
        {
            if (_vbo != 0)
            {
                try
                {
                    _gl.DeleteBuffer(_vbo);
                }
                catch
                {
                    // Сохраняем исходное исключение.
                }
            }

            if (_vao != 0)
            {
                try
                {
                    _gl.DeleteVertexArray(_vao);
                }
                catch
                {
                    // Сохраняем исходное исключение.
                }
            }

            try
            {
                _shader.Dispose();
            }
            catch
            {
                // Сохраняем исходное исключение.
            }

            throw;
        }
    }

    private bool PrepareTextureBatch(
    TextureHandle texture)
    {
        if (!texture.IsValid)
        {
            return false;
        }

        var value =
            texture.Value;

        if (_currentTexture != 0 &&
            _currentTexture != value)
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
                value;
        }

        return true;
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


    private void AddRegionClippedQuad(
        TextureHandle texture,
        float x,
        float y,
        float width,
        float height,
        Rectangle uv,
        ClipRegion region,
        float rotationDegrees)
    {
        if (width <= 0.0f ||
            height <= 0.0f ||
            region.IsEmpty)
        {
            return;
        }

        var origin = new Vector2(
            x + width * 0.5f,
            y + height * 0.5f);

        var quad = new[]
        {
        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x, y),
                origin,
                rotationDegrees),
            new Vector2(uv.X, uv.Y)),

        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x + width, y),
                origin,
                rotationDegrees),
            new Vector2(uv.X + uv.Width, uv.Y)),

        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x + width, y + height),
                origin,
                rotationDegrees),
            new Vector2(
                uv.X + uv.Width,
                uv.Y + uv.Height)),

        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x, y + height),
                origin,
                rotationDegrees),
            new Vector2(uv.X, uv.Y + uv.Height))
    };

        var clipped =
            UiPolygonClipper.ClipPolygon(
                quad,
                region);

        if (clipped.Count < 3)
        {
            return;
        }

        // Разбиваем выпуклый многоугольник на треугольники.
        for (var i = 1; i < clipped.Count - 1; i++)
        {
            if (!PrepareTextureBatch(texture))
            {
                return;
            }

            AddVertex(
                clipped[0].Position.X,
                clipped[0].Position.Y,
                clipped[0].UV.X,
                clipped[0].UV.Y);

            AddVertex(
                clipped[i].Position.X,
                clipped[i].Position.Y,
                clipped[i].UV.X,
                clipped[i].UV.Y);

            AddVertex(
                clipped[i + 1].Position.X,
                clipped[i + 1].Position.Y,
                clipped[i + 1].UV.X,
                clipped[i + 1].UV.Y);
        }
    }


    public void Draw(
    DrawUiTextureCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        if (!PrepareTextureBatch(command.Texture))
        {
            return;
        }

        if (command.ClipRegion is not null)
        {
            var region = UiPolygonClipper.Combine(
                command.ClipRegion,
                command.ClipRect);

            AddRegionClippedQuad(
                command.Texture,
                command.Position.X,
                command.Position.Y,
                command.Size.X,
                command.Size.Y,
                command.UV,
                region,
                command.RotationDegrees);

            return;
        }

        if (command.ClipRect.HasValue)
        {
            AddClippedQuad(
                command.Position.X,
                command.Position.Y,
                command.Size.X,
                command.Size.Y,
                command.UV,
                command.ClipRect.Value,
                command.RotationDegrees);

            return;
        }

        AddQuad(
            command.Position.X,
            command.Position.Y,
            command.Size.X,
            command.Size.Y,
            command.UV,
            command.RotationDegrees);
    }

    private void AddClippedQuad(
    float x,
    float y,
    float width,
    float height,
    Rectangle uv,
    Rectangle clip,
    float rotationDegrees = 0.0f,
    Vector2? rotationOrigin = null)
    {
        if (width <= 0.0f ||
            height <= 0.0f ||
            clip.Width <= 0.0f ||
            clip.Height <= 0.0f)
        {
            return;
        }

        // Запоминаем центр ДО обрезки.
        var origin = rotationOrigin ??
            new Vector2(
                x + width * 0.5f,
                y + height * 0.5f);

        var left = MathF.Max(x, clip.X);
        var top = MathF.Max(y, clip.Y);

        var right = MathF.Min(
            x + width,
            clip.X + clip.Width);

        var bottom = MathF.Min(
            y + height,
            clip.Y + clip.Height);

        if (right <= left ||
            bottom <= top)
        {
            return;
        }

        var leftRatio = (left - x) / width;
        var topRatio = (top - y) / height;
        var rightRatio = (right - x) / width;
        var bottomRatio = (bottom - y) / height;

        var clippedUv = new Rectangle(
            uv.X + uv.Width * leftRatio,
            uv.Y + uv.Height * topRatio,
            uv.Width * (rightRatio - leftRatio),
            uv.Height * (bottomRatio - topRatio));

        AddQuad(
            left,
            top,
            right - left,
            bottom - top,
            clippedUv,
            rotationDegrees,
            origin);
    }

    public void Draw(
        DrawTextureCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        DrawQuad(
            command.Texture,
            command.Position,
            command.Size,
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

        DrawQuad(
            command.Texture,
            position,
            size,
            command.UV,
            command.RotationDegrees);
    }

    private void DrawQuad(
        TextureHandle texture,
        Vector2 position,
        Vector2 size,
        Rectangle uv,
        float rotationDegrees = 0.0f)
    {
        if (!PrepareTextureBatch(texture))
        {
            return;
        }

        AddQuad(
            position.X,
            position.Y,
            size.X,
            size.Y,
            uv,
            rotationDegrees);
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
        Rectangle uv,
        float rotationDegrees = 0.0f,
        Vector2? rotationOrigin = null)
    {
        var right = x + width;
        var bottom = y + height;

        // По умолчанию вращаем вокруг центра прямоугольника.
        var origin = rotationOrigin ??
            new Vector2(
                x + width * 0.5f,
                y + height * 0.5f);

        // Вычисляем координаты четырёх углов с учётом поворота.
        var topLeft = RotatePoint(
            new Vector2(x, y),
            origin,
            rotationDegrees);

        var topRight = RotatePoint(
            new Vector2(right, y),
            origin,
            rotationDegrees);

        var bottomRight = RotatePoint(
            new Vector2(right, bottom),
            origin,
            rotationDegrees);

        var bottomLeft = RotatePoint(
            new Vector2(x, bottom),
            origin,
            rotationDegrees);

        // Первый треугольник.
        AddVertex(
            topLeft.X,
            topLeft.Y,
            uv.X,
            uv.Y);

        AddVertex(
            topRight.X,
            topRight.Y,
            uv.X + uv.Width,
            uv.Y);

        AddVertex(
            bottomRight.X,
            bottomRight.Y,
            uv.X + uv.Width,
            uv.Y + uv.Height);

        // Второй треугольник.
        AddVertex(
            topLeft.X,
            topLeft.Y,
            uv.X,
            uv.Y);

        AddVertex(
            bottomRight.X,
            bottomRight.Y,
            uv.X + uv.Width,
            uv.Y + uv.Height);

        AddVertex(
            bottomLeft.X,
            bottomLeft.Y,
            uv.X,
            uv.Y + uv.Height);
    }

    private static Vector2 RotatePoint(
    Vector2 point,
    Vector2 origin,
    float rotationDegrees)
    {
        if (MathF.Abs(rotationDegrees) < 0.0001f)
            return point;

        var radians =
            rotationDegrees * (MathF.PI / 180.0f);

        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - origin.X;
        var dy = point.Y - origin.Y;

        return new Vector2(
            origin.X + dx * cos - dy * sin,
            origin.Y + dx * sin + dy * cos);
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
        if (_vertexCount == 0 ||
            _currentTexture == 0)
        {
            return;
        }

        try
        {
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
        }
        finally
        {
            _gl.BindBuffer(
                BufferTargetARB.ArrayBuffer,
                0);
        }

        _vertexCount = 0;
        _currentTexture = 0;
    }

    private (uint Vao, uint Vbo) CreateBatchBuffer()
    {
        uint vao = 0;
        uint vbo = 0;

        try
        {
            vao = _gl.GenVertexArray();

            if (vao == 0)
            {
                throw new InvalidOperationException(
                    "OpenGL failed to create a vertex array.");
            }

            vbo = _gl.GenBuffer();

            if (vbo == 0)
            {
                throw new InvalidOperationException(
                    "OpenGL failed to create a vertex buffer.");
            }

            _gl.BindVertexArray(vao);

            _gl.BindBuffer(
                BufferTargetARB.ArrayBuffer,
                vbo);

            try
            {
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

                _gl.EnableVertexAttribArray(0);

                _gl.VertexAttribPointer(
                    1,
                    2,
                    VertexAttribPointerType.Float,
                    false,
                    stride,
                    2 * sizeof(float));

                _gl.EnableVertexAttribArray(1);
            }
            finally
            {
                _gl.BindBuffer(
                    BufferTargetARB.ArrayBuffer,
                    0);

                _gl.BindVertexArray(0);
            }

            return (vao, vbo);
        }
        catch
        {
            // Очистка не должна подменять исходную ошибку.
            try
            {
                _gl.BindBuffer(
                    BufferTargetARB.ArrayBuffer,
                    0);
            }
            catch
            {
            }

            try
            {
                _gl.BindVertexArray(0);
            }
            catch
            {
            }

            if (vbo != 0)
            {
                try
                {
                    _gl.DeleteBuffer(vbo);
                }
                catch
                {
                }
            }

            if (vao != 0)
            {
                try
                {
                    _gl.DeleteVertexArray(vao);
                }
                catch
                {
                }
            }

            throw;
        }
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