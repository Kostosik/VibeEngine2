using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Layout;
using Engine.UI.Styling;

namespace Engine.UI.Core;

public sealed class UiRenderContext
{
    private readonly Stack<Rectangle> _clipStack = new();
    private readonly IGraphicsDevice _graphics;

    public UiRenderContext(
        IGraphicsDevice graphics,
        UiTheme theme)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(theme);

        _graphics = graphics;
        Theme = theme;
    }

    public UiTheme Theme { get; }

    public int LayerOffset { get; set; }

    private readonly List<RotationTransform> _rotationTransforms = new();

    private readonly record struct RotationTransform(
        Vector2 Pivot,
        float Degrees);

    public void DrawRectangle(
    UiRect rect,
    UiColor color,
    bool filled = true,
    int layer = 0)
    {
        var center = TransformPoint(
            new Vector2(
                rect.X + rect.Width * 0.5f,
                rect.Y + rect.Height * 0.5f));

        var position = new Vector2(
            center.X - rect.Width * 0.5f,
            center.Y - rect.Height * 0.5f);

        _graphics.Submit(
            new DrawUiRectangleCommand(
                position,
                rect.Size,
                color,
                filled,
                1000 + LayerOffset + layer,
                CurrentClip,
                CurrentRotationDegrees));
    }

    public void DrawText(
        string text,
        Vector2 position,
        float fontSize,
        UiColor color,
        int layer = 0)
    {
        if (string.IsNullOrEmpty(text) ||
            fontSize <= 0.0f)
        {
            return;
        }

        var transformedPosition = TransformPoint(position);

        _graphics.Submit(
            new DrawUiTextCommand(
                text,
                Theme.DefaultFont,
                transformedPosition,
                fontSize,
                color,
                1001 + LayerOffset + layer,
                CurrentClip,
                CurrentRotationDegrees));
    }

    public void PushRotation(
    Vector2 pivot,
    float degrees)
    {
        if (!float.IsFinite(pivot.X) ||
            !float.IsFinite(pivot.Y) ||
            !float.IsFinite(degrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(degrees),
                "UI rotation transform must be finite.");
        }

        _rotationTransforms.Add(
            new RotationTransform(pivot, degrees));
    }

    public void PopRotation()
    {
        if (_rotationTransforms.Count == 0)
        {
            throw new InvalidOperationException(
                "UI rotation stack is empty.");
        }

        _rotationTransforms.RemoveAt(
            _rotationTransforms.Count - 1);
    }

    private float CurrentRotationDegrees
    {
        get
        {
            var degrees = 0.0f;

            foreach (var transform in _rotationTransforms)
            {
                degrees += transform.Degrees;
            }

            return degrees;
        }
    }

    private Vector2 TransformPoint(Vector2 point)
    {
        // Сначала применяем самый внутренний transform,
        // затем последовательно трансформации родителей.
        for (var i = _rotationTransforms.Count - 1; i >= 0; i--)
        {
            var transform = _rotationTransforms[i];

            point = RotatePoint(
                point,
                transform.Pivot,
                transform.Degrees);
        }

        return point;
    }

    private static Vector2 RotatePoint(
        Vector2 point,
        Vector2 pivot,
        float degrees)
    {
        var radians = degrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var x = point.X - pivot.X;
        var y = point.Y - pivot.Y;

        return new Vector2(
            pivot.X + x * cos - y * sin,
            pivot.Y + x * sin + y * cos);
    }
    public void PushClip(
    UiRect rect)
    {
        var clip =
            new Rectangle(
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height);

        if (_clipStack.Count > 0)
        {
            clip =
                Intersect(
                    _clipStack.Peek(),
                    clip);
        }

        _clipStack.Push(
            clip);
    }

    public void PopClip()
    {
        if (_clipStack.Count == 0)
        {
            throw new InvalidOperationException(
                "UI clip stack is empty.");
        }

        _clipStack.Pop();
    }

    private Rectangle? CurrentClip =>
        _clipStack.Count > 0
            ? _clipStack.Peek()
            : null;

    private static Rectangle Intersect(
        Rectangle first,
        Rectangle second)
    {
        var left =
            MathF.Max(
                first.X,
                second.X);

        var top =
            MathF.Max(
                first.Y,
                second.Y);

        var right =
            MathF.Min(
                first.X + first.Width,
                second.X + second.Width);

        var bottom =
            MathF.Min(
                first.Y + first.Height,
                second.Y + second.Height);

        return new Rectangle(
            left,
            top,
            MathF.Max(
                0.0f,
                right - left),
            MathF.Max(
                0.0f,
                bottom - top));
    }


    public Vector2 MeasureText(
    string text,
    float fontSize)
    {
        if (string.IsNullOrEmpty(text) ||
            fontSize <= 0.0f ||
            !Theme.DefaultFont.IsValid)
        {
            return Vector2.Zero;
        }

        var metrics =
            _graphics.Fonts.MeasureText(
                Theme.DefaultFont,
                text);

        if (metrics.LineHeight <= 0.0f)
        {
            return Vector2.Zero;
        }

        var scale =
            fontSize /
            metrics.LineHeight;

        return new Vector2(
            metrics.Width * scale,
            metrics.Height * scale);
    }

    public void DrawTexture(
        TextureHandle texture,
        UiRect rect,
        Rectangle uv,
        int layer = 0)
    {
        if (!texture.IsValid ||
            rect.Width <= 0.0f ||
            rect.Height <= 0.0f)
        {
            return;
        }

        var center = TransformPoint(
            new Vector2(
                rect.X + rect.Width * 0.5f,
                rect.Y + rect.Height * 0.5f));

        var position = new Vector2(
            center.X - rect.Width * 0.5f,
            center.Y - rect.Height * 0.5f);

        _graphics.Submit(
            new DrawUiTextureCommand(
                texture,
                position,
                rect.Size,
                uv,
                1000 + LayerOffset + layer,
                CurrentClip,
                CurrentRotationDegrees));
    }
}