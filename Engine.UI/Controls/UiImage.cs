using Engine.Core.Math;
using Engine.Graphics.Resources;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiImage : UiWidget
{
    public UiImage(
        TextureHandle texture,
        Vector2 size)
    {
        Texture = texture;
        Size = size;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;
    }

    public TextureHandle Texture { get; set; }

    public Vector2 Size { get; set; }

    public bool PreserveAspectRatio { get; set; } = true;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return Size;
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        if (!Texture.IsValid)
        {
            return;
        }

        var destination =
            Bounds;

        if (PreserveAspectRatio &&
            Size.X > 0.0f &&
            Size.Y > 0.0f)
        {
            destination =
                CalculateAspectFit(
                    Bounds,
                    Size);
        }

        context.DrawTexture(
            Texture,
            destination,
            new Rectangle(
                0.0f,
                0.0f,
                1.0f,
                1.0f));
    }

    private static UiRect CalculateAspectFit(
        UiRect bounds,
        Vector2 imageSize)
    {
        var scale =
            MathF.Min(
                bounds.Width / imageSize.X,
                bounds.Height / imageSize.Y);

        var width =
            imageSize.X * scale;

        var height =
            imageSize.Y * scale;

        return new UiRect(
            bounds.X +
            (bounds.Width - width) /
            2.0f,

            bounds.Y +
            (bounds.Height - height) /
            2.0f,

            width,
            height);
    }
}