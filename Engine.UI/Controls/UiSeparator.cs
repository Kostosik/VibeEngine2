using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public enum UiSeparatorOrientation
{
    Horizontal,
    Vertical
}

public sealed class UiSeparator : UiWidget
{
    public UiSeparator(
        UiSeparatorOrientation orientation =
            UiSeparatorOrientation.Horizontal)
    {
        Orientation =
            orientation;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;
    }

    public UiSeparatorOrientation Orientation { get; set; }

    public float Thickness { get; set; } = 1.0f;

    public float Length { get; set; } = 200.0f;

    public UiColor Color { get; set; } =
        new(80, 80, 80, 255);

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return Orientation ==
               UiSeparatorOrientation.Horizontal
            ? new Vector2(
                Length,
                Thickness)
            : new Vector2(
                Thickness,
                Length);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        context.DrawRectangle(
            Bounds,
            Color,
            filled: true);
    }
}