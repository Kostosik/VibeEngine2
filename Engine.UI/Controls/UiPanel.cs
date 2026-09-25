using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public class UiPanel : UiContainer
{
    public UiThickness Padding { get; set; } =
        UiThickness.Zero;

    public UiColor Background { get; set; } =
        UiColor.Transparent;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var contentAvailable =
            new Vector2(
                MathF.Max(
                    0.0f,
                    availableSize.X -
                    Padding.Left -
                    Padding.Right),
                MathF.Max(
                    0.0f,
                    availableSize.Y -
                    Padding.Top -
                    Padding.Bottom));

        var width = 0.0f;
        var height = 0.0f;

        foreach (var child in Children)
        {
            child.Measure(
                context,
                contentAvailable);

            width =
                MathF.Max(
                    width,
                    child.DesiredSize.X);

            height =
                MathF.Max(
                    height,
                    child.DesiredSize.Y);
        }

        return new Vector2(
            width +
            Padding.Left +
            Padding.Right,
            height +
            Padding.Top +
            Padding.Bottom);
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var content =
            finalRect.Deflate(
                Padding);

        foreach (var child in Children)
        {
            child.Arrange(
                content);
        }
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        if (Background == UiColor.Transparent)
        {
            return;
        }

        context.DrawRectangle(
            Bounds,
            Background,
            filled: true);
    }
}