using Engine.Graphics.Cameras;
using Engine.Graphics.Commands;
using Engine.Graphics.Debug;
using Engine.Core.Math;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLDebugRenderer :
    IDisposable
{
    private readonly GL _gl;
    private readonly Camera _camera;
    private readonly OpenGLShaderProgram _shader;

    private readonly uint _vao;
    private readonly uint _vbo;

    private readonly int _resolutionLocation;

    private bool _frameActive;
    private bool _disposed;

    public OpenGLDebugRenderer(
        GL gl,
        int width,
        int height,
        Camera camera)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(camera);

        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        _gl = gl;
        _camera = camera;

        _shader =
            new OpenGLShaderProgram(
                _gl,
                VertexShaderSource,
                FragmentShaderSource);

        _resolutionLocation =
            _shader.GetUniformLocation(
                "uResolution");

        if (_resolutionLocation < 0)
        {
            throw new InvalidOperationException(
                "Uniform 'uResolution' was not found.");
        }

        (_vao, _vbo) =
            CreateBuffer();

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
                "Debug renderer frame has already begun.");
        }

        _shader.Use();

        _gl.Uniform2(
            _resolutionLocation,
            _width,
            _height);

        _gl.BindVertexArray(
            _vao);

        _frameActive = true;
    }



    public void Draw(
    DrawDebugTextCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        if (string.IsNullOrEmpty(command.Text) ||
            command.Scale <= 0.0f)
        {
            return;
        }

        DrawText(
            command);
    }

    public void Draw(
        DrawDebugLineCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        var start =
            ToScreen(
                command.Start,
                command.Space);

        var end =
            ToScreen(
                command.End,
                command.Space);

        DrawLine(
            start,
            end,
            command.Color);
    }

    public void Draw(
        DrawDebugRectangleCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        var position =
            ToScreen(
                command.Position,
                command.Space);

        var size =
            command.Space ==
            DebugRenderSpace.World
                ? command.Size *
                  _camera.Zoom
                : command.Size;

        var topLeft =
            position;

        var topRight =
            new Vector2(
                position.X + size.X,
                position.Y);

        var bottomRight =
            position + size;

        var bottomLeft =
            new Vector2(
                position.X,
                position.Y + size.Y);

        if (command.Filled)
        {
            DrawTriangle(
                topLeft,
                topRight,
                bottomRight,
                command.Color);

            DrawTriangle(
                topLeft,
                bottomRight,
                bottomLeft,
                command.Color);

            return;
        }

        DrawLine(
            topLeft,
            topRight,
            command.Color);

        DrawLine(
            topRight,
            bottomRight,
            command.Color);

        DrawLine(
            bottomRight,
            bottomLeft,
            command.Color);

        DrawLine(
            bottomLeft,
            topLeft,
            command.Color);
    }

    public void Draw(
    DrawUiRectangleCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        if (command.Size.X <= 0.0f ||
            command.Size.Y <= 0.0f)
        {
            return;
        }

        var position =
            command.Position;

        var size =
            command.Size;

        if (command.ClipRect.HasValue)
        {
            var clip =
                command.ClipRect.Value;

            var left =
                MathF.Max(
                    position.X,
                    clip.X);

            var top =
                MathF.Max(
                    position.Y,
                    clip.Y);

            var right =
                MathF.Min(
                    position.X + size.X,
                    clip.X + clip.Width);

            var bottom =
                MathF.Min(
                    position.Y + size.Y,
                    clip.Y + clip.Height);

            if (right <= left ||
                bottom <= top)
            {
                return;
            }

            position =
                new Vector2(
                    left,
                    top);

            size =
                new Vector2(
                    right - left,
                    bottom - top);
        }

        var topLeft =
            position;

        var topRight =
            new Vector2(
                position.X + size.X,
                position.Y);

        var bottomRight =
            position + size;

        var bottomLeft =
            new Vector2(
                position.X,
                position.Y + size.Y);

        if (command.Filled)
        {
            DrawTriangle(
                topLeft,
                topRight,
                bottomRight,
                command.Color);

            DrawTriangle(
                topLeft,
                bottomRight,
                bottomLeft,
                command.Color);

            return;
        }

        DrawLine(
            topLeft,
            topRight,
            command.Color);

        DrawLine(
            topRight,
            bottomRight,
            command.Color);

        DrawLine(
            bottomRight,
            bottomLeft,
            command.Color);

        DrawLine(
            bottomLeft,
            topLeft,
            command.Color);
    }


    public void Draw(
        DrawDebugCircleCommand command)
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        if (command.Radius <= 0.0f)
            return;

        var segments =
            Math.Max(
                3,
                command.Segments);

        var center =
            ToScreen(
                command.Center,
                command.Space);

        var radius =
            command.Space ==
            DebugRenderSpace.World
                ? command.Radius *
                  _camera.Zoom
                : command.Radius;

        const float TwoPi =
            MathF.PI * 2.0f;

        for (var i = 0;
             i < segments;
             i++)
        {
            var startAngle =
                TwoPi *
                i /
                segments;

            var endAngle =
                TwoPi *
                (i + 1) /
                segments;

            var start =
                center +
                new Vector2(
                    MathF.Cos(startAngle) * radius,
                    MathF.Sin(startAngle) * radius);

            var end =
                center +
                new Vector2(
                    MathF.Cos(endAngle) * radius,
                    MathF.Sin(endAngle) * radius);

            if (command.Filled)
            {
                DrawTriangle(
                    center,
                    start,
                    end,
                    command.Color);
            }
            else
            {
                DrawLine(
                    start,
                    end,
                    command.Color);
            }
        }
    }

    private void DrawText(
    string text,
    Vector2 position,
    float scale,
    UiColor color)
    {
        var x =
            position.X;

        var y =
            position.Y;

        const float GlyphWidth = 5.0f;
        const float GlyphHeight = 7.0f;
        const float GlyphSpacing = 1.0f;

        var vertices =
            new List<float>(
                text.Length *
                5 *
                7 *
                6 *
                6);

        foreach (var character in text)
        {
            var glyph =
                GetGlyph(character);

            for (var row = 0;
                 row < GlyphHeight;
                 row++)
            {
                var bits =
                    glyph[(int)row];

                for (var column = 0;
                     column < GlyphWidth;
                     column++)
                {
                    var mask =
                        1 << (int)(GlyphWidth - 1 - column);

                    if ((bits & mask) == 0)
                    {
                        continue;
                    }

                    var pixelX =
                        x +
                        column *
                        scale;

                    var pixelY =
                        y +
                        row *
                        scale;

                    AddTextQuad(
                        vertices,
                        pixelX,
                        pixelY,
                        scale,
                        scale,
                        color);
                }
            }

            x +=
                (GlyphWidth + GlyphSpacing) *
                scale;
        }

        if (vertices.Count == 0)
        {
            return;
        }

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vbo);

        var vertexData =
            vertices.ToArray();

        _gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            vertexData,
            BufferUsageARB.DynamicDraw);

        _gl.DrawArrays(
            PrimitiveType.Triangles,
            0,
            (uint)(vertexData.Length / 6));

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);
    }

    private void DrawText(
    DrawDebugTextCommand command)
    {
        var position =
            ToScreen(
                command.Position,
                command.Space);

        var scale =
            command.Scale;

        var x =
            position.X;

        var y =
            position.Y;

        const float GlyphWidth = 5.0f;
        const float GlyphHeight = 7.0f;
        const float GlyphSpacing = 1.0f;

        var vertices =
            new List<float>(
                command.Text.Length *
                5 *
                7 *
                6 *
                6);

        foreach (var character in command.Text)
        {
            var glyph =
                GetGlyph(character);

            for (var row = 0;
                 row < GlyphHeight;
                 row++)
            {
                var bits =
                    glyph[(int)row];

                for (var column = 0;
                     column < GlyphWidth;
                     column++)
                {
                    var mask =
                        1 << (int)(GlyphWidth - 1 - column);

                    if ((bits & mask) == 0)
                    {
                        continue;
                    }

                    var pixelX =
                        x +
                        column *
                        scale;

                    var pixelY =
                        y +
                        row *
                        scale;

                    var width =
                        scale;

                    var height =
                        scale;

                    AddTextQuad(
                        vertices,
                        pixelX,
                        pixelY,
                        width,
                        height,
                        command.Color);
                }
            }

            x +=
                (GlyphWidth + GlyphSpacing) *
                scale;
        }

        if (vertices.Count == 0)
        {
            return;
        }

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vbo);

        var vertexData =
            vertices.ToArray();

        _gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            vertexData,
            BufferUsageARB.DynamicDraw);

        _gl.DrawArrays(
            PrimitiveType.Triangles,
            0,
            (uint)(vertexData.Length / 6));

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);
    }

    private void AddTextQuad(
    List<float> vertices,
    float x,
    float y,
    float width,
    float height,
    UiColor color)
    {
        var right =
            x + width;

        var bottom =
            y + height;

        AddTextVertex(
            vertices,
            x,
            y,
            color);

        AddTextVertex(
            vertices,
            right,
            y,
            color);

        AddTextVertex(
            vertices,
            right,
            bottom,
            color);

        AddTextVertex(
            vertices,
            x,
            y,
            color);

        AddTextVertex(
            vertices,
            right,
            bottom,
            color);

        AddTextVertex(
            vertices,
            x,
            bottom,
            color);
    }

    private void DrawLine(
    Vector2 start,
    Vector2 end,
    UiColor color)
    {
        DrawVertices(
            PrimitiveType.Lines,
            new[]
            {
            start.X,
            start.Y,
            ToFloat(color.R),
            ToFloat(color.G),
            ToFloat(color.B),
            ToFloat(color.A),

            end.X,
            end.Y,
            ToFloat(color.R),
            ToFloat(color.G),
            ToFloat(color.B),
            ToFloat(color.A)
            });
    }

    private void DrawTriangle(
    Vector2 first,
    Vector2 second,
    Vector2 third,
    UiColor color)
    {
        DrawVertices(
            PrimitiveType.Triangles,
            new[]
            {
            first.X,
            first.Y,
            ToFloat(color.R),
            ToFloat(color.G),
            ToFloat(color.B),
            ToFloat(color.A),

            second.X,
            second.Y,
            ToFloat(color.R),
            ToFloat(color.G),
            ToFloat(color.B),
            ToFloat(color.A),

            third.X,
            third.Y,
            ToFloat(color.R),
            ToFloat(color.G),
            ToFloat(color.B),
            ToFloat(color.A)
            });
    }

    private static void AddTextVertex(
    List<float> vertices,
    float x,
    float y,
    UiColor color)
    {
        vertices.Add(x);
        vertices.Add(y);

        vertices.Add(
            color.R / 255.0f);

        vertices.Add(
            color.G / 255.0f);

        vertices.Add(
            color.B / 255.0f);

        vertices.Add(
            color.A / 255.0f);
    }

    private void AddTextQuad(
        List<float> vertices,
        float x,
        float y,
        float width,
        float height,
        DebugColor color)
    {
        var right =
            x + width;

        var bottom =
            y + height;

        AddTextVertex(
            vertices,
            x,
            y,
            color);

        AddTextVertex(
            vertices,
            right,
            y,
            color);

        AddTextVertex(
            vertices,
            right,
            bottom,
            color);

        AddTextVertex(
            vertices,
            x,
            y,
            color);

        AddTextVertex(
            vertices,
            right,
            bottom,
            color);

        AddTextVertex(
            vertices,
            x,
            bottom,
            color);
    }

    private static byte[] GetGlyph(
    char character)
    {
        character =
            char.ToUpperInvariant(
                character);

        return character switch
        {
            'A' => [14, 17, 17, 31, 17, 17, 17],
            'B' => [30, 17, 17, 30, 17, 17, 30],
            'C' => [14, 17, 16, 16, 16, 17, 14],
            'D' => [30, 17, 17, 17, 17, 17, 30],
            'E' => [31, 16, 16, 30, 16, 16, 31],
            'F' => [31, 16, 16, 30, 16, 16, 16],
            'G' => [14, 17, 16, 23, 17, 17, 15],
            'H' => [17, 17, 17, 31, 17, 17, 17],
            'I' => [31, 4, 4, 4, 4, 4, 31],
            'J' => [7, 2, 2, 2, 2, 18, 12],
            'K' => [17, 18, 20, 24, 20, 18, 17],
            'L' => [16, 16, 16, 16, 16, 16, 31],
            'M' => [17, 27, 21, 21, 17, 17, 17],
            'N' => [17, 25, 21, 19, 17, 17, 17],
            'O' => [14, 17, 17, 17, 17, 17, 14],
            'P' => [30, 17, 17, 30, 16, 16, 16],
            'Q' => [14, 17, 17, 17, 21, 18, 13],
            'R' => [30, 17, 17, 30, 20, 18, 17],
            'S' => [15, 16, 16, 14, 1, 1, 30],
            'T' => [31, 4, 4, 4, 4, 4, 4],
            'U' => [17, 17, 17, 17, 17, 17, 14],
            'V' => [17, 17, 17, 17, 17, 10, 4],
            'W' => [17, 17, 17, 21, 21, 21, 10],
            'X' => [17, 17, 10, 4, 10, 17, 17],
            'Y' => [17, 17, 10, 4, 4, 4, 4],
            'Z' => [31, 1, 2, 4, 8, 16, 31],

            '0' => [14, 17, 19, 21, 25, 17, 14],
            '1' => [4, 12, 4, 4, 4, 4, 14],
            '2' => [14, 17, 1, 2, 4, 8, 31],
            '3' => [30, 1, 1, 14, 1, 1, 30],
            '4' => [2, 6, 10, 18, 31, 2, 2],
            '5' => [31, 16, 16, 30, 1, 1, 30],
            '6' => [14, 16, 16, 30, 17, 17, 14],
            '7' => [31, 1, 2, 4, 8, 8, 8],
            '8' => [14, 17, 17, 14, 17, 17, 14],
            '9' => [14, 17, 17, 15, 1, 1, 14],

            ' ' => [0, 0, 0, 0, 0, 0, 0],
            '.' => [0, 0, 0, 0, 0, 12, 12],
            ':' => [0, 12, 12, 0, 12, 12, 0],
            '!' => [4, 4, 4, 4, 4, 0, 4],
            '?' => [14, 17, 1, 2, 4, 0, 4],
            '-' => [0, 0, 0, 31, 0, 0, 0],
            '_' => [0, 0, 0, 0, 0, 0, 31],
            '/' => [1, 2, 2, 4, 8, 8, 16],
            '\'' => [4, 4, 8, 0, 0, 0, 0],
            '"' => [10, 10, 10, 0, 0, 0, 0],
            ',' => [0, 0, 0, 0, 0, 12, 8],
            '>' => [16, 8, 4, 2, 4, 8, 16],
            '<' => [1, 2, 4, 8, 4, 2, 1],
            '(' => [2, 4, 8, 8, 8, 4, 2],
            ')' => [8, 4, 2, 2, 2, 4, 8],
            '[' => [14, 8, 8, 8, 8, 8, 14],
            ']' => [14, 2, 2, 2, 2, 2, 14],
            '=' => [0, 31, 0, 31, 0, 0, 0],

            _ => [14, 17, 1, 2, 4, 0, 4]
        };
    }

    private static void AddTextVertex(
        List<float> vertices,
        float x,
        float y,
        DebugColor color)
    {
        vertices.Add(x);
        vertices.Add(y);

        vertices.Add(
            color.R / 255.0f);

        vertices.Add(
            color.G / 255.0f);

        vertices.Add(
            color.B / 255.0f);

        vertices.Add(
            color.A / 255.0f);
    }

    public void End()
    {
        ThrowIfDisposed();
        EnsureFrameActive();

        _gl.BindVertexArray(0);

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

        _width = width;
        _height = height;

        if (_frameActive)
        {
            _gl.Uniform2(
                _resolutionLocation,
                _width,
                _height);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _shader.Dispose();

        _gl.DeleteBuffer(
            _vbo);

        _gl.DeleteVertexArray(
            _vao);

        _disposed = true;
    }

    private Vector2 ToScreen(
        Vector2 position,
        DebugRenderSpace space)
    {
        return space ==
               DebugRenderSpace.World
            ? _camera.WorldToScreen(
                position)
            : position;
    }

    private void DrawLine(
        Vector2 start,
        Vector2 end,
        DebugColor color)
    {
        DrawVertices(
            PrimitiveType.Lines,
            new[]
            {
                start.X,
                start.Y,
                ToFloat(color.R),
                ToFloat(color.G),
                ToFloat(color.B),
                ToFloat(color.A),

                end.X,
                end.Y,
                ToFloat(color.R),
                ToFloat(color.G),
                ToFloat(color.B),
                ToFloat(color.A)
            });
    }

    private void DrawTriangle(
        Vector2 first,
        Vector2 second,
        Vector2 third,
        DebugColor color)
    {
        DrawVertices(
            PrimitiveType.Triangles,
            new[]
            {
                first.X,
                first.Y,
                ToFloat(color.R),
                ToFloat(color.G),
                ToFloat(color.B),
                ToFloat(color.A),

                second.X,
                second.Y,
                ToFloat(color.R),
                ToFloat(color.G),
                ToFloat(color.B),
                ToFloat(color.A),

                third.X,
                third.Y,
                ToFloat(color.R),
                ToFloat(color.G),
                ToFloat(color.B),
                ToFloat(color.A)
            });
    }

    private void DrawVertices(
        PrimitiveType primitiveType,
        float[] vertices)
    {
        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vbo);

        _gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            vertices,
            BufferUsageARB.DynamicDraw);

        _gl.DrawArrays(
            primitiveType,
            0,
            (uint)(vertices.Length / 6));

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);
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
            6 * sizeof(float);

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
            4,
            VertexAttribPointerType.Float,
            false,
            stride,
            2 * sizeof(float));

        _gl.EnableVertexAttribArray(1);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);

        _gl.BindVertexArray(
            0);

        return (
            vao,
            vbo);
    }

    private static float ToFloat(
        byte value)
    {
        return value / 255.0f;
    }

    private void EnsureFrameActive()
    {
        if (!_frameActive)
        {
            throw new InvalidOperationException(
                "Debug renderer frame has not begun.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private float _width;
    private float _height;

    private const string VertexShaderSource = """
        #version 330 core

        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec4 aColor;

        uniform vec2 uResolution;

        out vec4 vColor;

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

            vColor =
                aColor;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core

        in vec4 vColor;

        out vec4 FragColor;

        void main()
        {
            FragColor =
                vColor;
        }
        """;
}