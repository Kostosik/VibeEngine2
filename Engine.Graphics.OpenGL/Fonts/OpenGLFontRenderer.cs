using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Fonts;

internal sealed class OpenGLFontRenderer :
    IDisposable
{
    private readonly GL _gl;
    private readonly IFontManager _fonts;
    private readonly OpenGLShaderProgram _shader;

    private readonly uint _vao;
    private readonly uint _vbo;

    private readonly int _resolutionLocation;
    private readonly int _textureLocation;
    private readonly int _colorLocation;

    private readonly List<float> _vertices =
        new(4096);

    private float _width;
    private float _height;

    private bool _frameActive;
    private bool _disposed;

    private uint _currentTexture;

    public OpenGLFontRenderer(
        GL gl,
        int width,
        int height,
        IFontManager fonts)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(fonts);

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height));
        }

        _gl = gl;
        _fonts = fonts;

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

            _colorLocation =
                _shader.GetUniformLocation(
                    "uColor");

            ValidateUniforms();

            (_vao, _vbo) =
                CreateBuffer();
        }
        catch
        {
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

    public void Begin()
    {
        ThrowIfDisposed();

        if (_frameActive)
        {
            throw new InvalidOperationException(
                "Font renderer frame has already begun.");
        }

        _vertices.Clear();

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

        // Для текста сохраняем переданную точку вращения.
        // Если её нет, используем центр исходного прямоугольника.
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


    private void AddRegionClippedGlyph(
        float x,
        float y,
        float width,
        float height,
        Rectangle uv,
        ClipRegion region,
        float rotationDegrees,
        Vector2 rotationOrigin)
    {
        if (width <= 0.0f ||
            height <= 0.0f ||
            region.IsEmpty)
        {
            return;
        }

        var quad = new[]
        {
        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x, y),
                rotationOrigin,
                rotationDegrees),
            new Vector2(uv.X, uv.Y)),

        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x + width, y),
                rotationOrigin,
                rotationDegrees),
            new Vector2(uv.X + uv.Width, uv.Y)),

        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x + width, y + height),
                rotationOrigin,
                rotationDegrees),
            new Vector2(
                uv.X + uv.Width,
                uv.Y + uv.Height)),

        new UiClipVertex(
            UiPolygonClipper.RotatePoint(
                new Vector2(x, y + height),
                rotationOrigin,
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

        for (var i = 1; i < clipped.Count - 1; i++)
        {
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
        DrawUiTextCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        if (string.IsNullOrEmpty(command.Text) ||
            command.FontSize <= 0.0f ||
            !_fonts.Exists(command.Font))
        {
            return;
        }

        var metrics =
            _fonts.GetMetrics(
                command.Font);

        if (metrics.LineHeight <= 0.0f)
        {
            return;
        }

        var scale =
            command.FontSize /
            metrics.LineHeight;

        _gl.Uniform4(
            _colorLocation,
            command.Color.R / 255.0f,
            command.Color.G / 255.0f,
            command.Color.B / 255.0f,
            command.Color.A / 255.0f);

        var baseline =
            command.Position.Y +
            metrics.Ascent * scale;

        var x =
            command.Position.X;

        foreach (var rune in command.Text.EnumerateRunes())
        {
            if (rune.Value == '\n')
            {
                x = command.Position.X;

                baseline +=
                    metrics.LineHeight * scale;

                continue;
            }

            if (rune.Value == '\r')
            {
                continue;
            }

            var glyph =
                _fonts.GetGlyph(
                    command.Font,
                    rune.Value);

            if (glyph.Texture.IsValid &&
                glyph.Size.X > 0.0f &&
                glyph.Size.Y > 0.0f)
            {
                var texture =
                    glyph.Texture.Value;

                if (_currentTexture != 0 &&
                    _currentTexture != texture)
                {
                    Flush();
                }

                _currentTexture =
                    texture;

                var glyphX =
                    x + glyph.Bearing.X * scale;

                var glyphY =
                    baseline + glyph.Bearing.Y * scale;

                var glyphWidth =
                    glyph.Size.X * scale;

                var glyphHeight =
                    glyph.Size.Y * scale;

                if (command.ClipRegion is not null)
                {
                    var region =
                        UiPolygonClipper.Combine(
                            command.ClipRegion,
                            command.ClipRect);

                    AddRegionClippedGlyph(
                        glyphX,
                        glyphY,
                        glyphWidth,
                        glyphHeight,
                        glyph.UV,
                        region,
                        command.RotationDegrees,
                        command.Position);
                }
                else if (command.ClipRect.HasValue)
                {
                    AddClippedQuad(
                        glyphX,
                        glyphY,
                        glyphWidth,
                        glyphHeight,
                        glyph.UV,
                        command.ClipRect.Value,
                        command.RotationDegrees,
                        command.Position);
                }
                else
                {
                    AddQuad(
                        glyphX,
                        glyphY,
                        glyphWidth,
                        glyphHeight,
                        glyph.UV,
                        command.RotationDegrees,
                        command.Position);
                }
            }

            x += glyph.Advance * scale;
        }

        Flush();
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
        {
            throw new ArgumentOutOfRangeException(
                nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height));
        }

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
        Engine.Core.Math.Rectangle uv,
        float rotationDegrees = 0.0f,
        Vector2? rotationOrigin = null)
    {
        var right = x + width;
        var bottom = y + height;

        var origin = rotationOrigin ??
            new Vector2(
                x + width * 0.5f,
                y + height * 0.5f);

        var topLeft =
            UiPolygonClipper.RotatePoint(
                new Vector2(x, y),
                origin,
                rotationDegrees);

        var topRight =
            UiPolygonClipper.RotatePoint(
                new Vector2(right, y),
                origin,
                rotationDegrees);

        var bottomRight =
            UiPolygonClipper.RotatePoint(
                new Vector2(right, bottom),
                origin,
                rotationDegrees);

        var bottomLeft =
            UiPolygonClipper.RotatePoint(
                new Vector2(x, bottom),
                origin,
                rotationDegrees);

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
        {
            return point;
        }

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
        _vertices.Add(x);
        _vertices.Add(y);
        _vertices.Add(u);
        _vertices.Add(v);
    }

    private void Flush()
    {
        if (_vertices.Count == 0 ||
            _currentTexture == 0)
        {
            return;
        }

        _gl.BindTexture(
            TextureTarget.Texture2D,
            _currentTexture);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vbo);

        _gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            _vertices.ToArray(),
            BufferUsageARB.DynamicDraw);

        _gl.DrawArrays(
            PrimitiveType.Triangles,
            0,
            (uint)(_vertices.Count / 4));

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);

        _vertices.Clear();
        _currentTexture = 0;
    }

    private (uint Vao, uint Vbo) CreateBuffer()
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

            _gl.BindBuffer(
                BufferTargetARB.ArrayBuffer,
                0);

            _gl.BindVertexArray(0);

            return (vao, vbo);
        }
        catch
        {
            try
            {
                _gl.BindBuffer(
                    BufferTargetARB.ArrayBuffer,
                    0);
            }
            catch
            {
                // Сохраняем исходное исключение.
            }

            try
            {
                _gl.BindVertexArray(0);
            }
            catch
            {
                // Сохраняем исходное исключение.
            }

            if (vbo != 0)
            {
                try
                {
                    _gl.DeleteBuffer(vbo);
                }
                catch
                {
                    // Сохраняем исходное исключение.
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
                    // Сохраняем исходное исключение.
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

        if (_colorLocation < 0)
        {
            throw new InvalidOperationException(
                "Uniform 'uColor' was not found.");
        }
    }

    private void EnsureFrameActive()
    {
        if (!_frameActive)
        {
            throw new InvalidOperationException(
                "Font renderer frame has not begun.");
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
        uniform vec4 uColor;

        out vec4 FragColor;

        void main()
        {
            float alpha =
                texture(
                    uTexture,
                    vTexCoord).a;

            FragColor =
                vec4(
                    uColor.rgb,
                    uColor.a * alpha);
        }
        """;
}