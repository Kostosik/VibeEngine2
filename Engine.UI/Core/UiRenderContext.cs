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

    public void DrawRectangle(
        UiRect rect,
        UiColor color,
        bool filled = true,
        int layer = 0)
    {
        _graphics.Submit(
            new DrawUiRectangleCommand(
                rect.Position,
                rect.Size,
                color,
                filled,
                1000 + LayerOffset + layer,
                CurrentClip));
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

        _graphics.Submit(
            new DrawUiTextCommand(
                text,
                Theme.DefaultFont,
                position,
                fontSize,
                color,
                1001 + LayerOffset + layer,
                CurrentClip));
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

        _graphics.Submit(
            new DrawUiTextureCommand(
                texture,
                rect.Position,
                rect.Size,
                uv,
                1000 + LayerOffset + layer,
                CurrentClip));
    }
}