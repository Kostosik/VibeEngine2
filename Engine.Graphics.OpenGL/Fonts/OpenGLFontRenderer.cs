using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Rendering;
using Silk.NET.OpenGL;
using Engine.Core.Math;
using System.Text;

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
    Rectangle clip)
    {
        if (width <= 0.0f ||
            height <= 0.0f)
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
            metrics.Ascent *
            scale;

        var x =
            command.Position.X;

        foreach (var rune in command.Text.EnumerateRunes())
        {
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
                    x +
                    glyph.Bearing.X *
                    scale;

                var glyphY =
                    baseline +
                    glyph.Bearing.Y *
                    scale;

                var glyphWidth =
                    glyph.Size.X *
                    scale;

                var glyphHeight =
                    glyph.Size.Y *
                    scale;

                if (command.ClipRect.HasValue)
                {
                    AddClippedQuad(
                        glyphX,
                        glyphY,
                        glyphWidth,
                        glyphHeight,
                        glyph.UV,
                        command.ClipRect.Value);
                }
                else
                {
                    AddQuad(
                        glyphX,
                        glyphY,
                        glyphWidth,
                        glyphHeight,
                        glyph.UV);
                }
            }

            x +=
                glyph.Advance *
                scale;
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
        Engine.Core.Math.Rectangle uv)
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
        var vao =
            _gl.GenVertexArray();

        var vbo =
            _gl.GenBuffer();

        _gl.BindVertexArray(
            vao);

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