using Engine.Core.Math;
using Engine.UI.Core;

namespace Engine.UI.Layout;

public enum UiOrientation
{
    Vertical,
    Horizontal
}

public sealed class UiStackPanel : UiContainer
{
    public UiOrientation Orientation { get; set; } =
        UiOrientation.Vertical;

    public float Spacing { get; set; }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var width = 0.0f;
        var height = 0.0f;
        var childCount = 0;

        foreach (var child in Children)
        {
            var childAvailable =
                Orientation ==
                UiOrientation.Vertical
                    ? new Vector2(
                        availableSize.X,
                        float.PositiveInfinity)
                    : new Vector2(
                        float.PositiveInfinity,
                        availableSize.Y);

            child.Measure(
                context,
                childAvailable);

            if (Orientation ==
                UiOrientation.Vertical)
            {
                width =
                    MathF.Max(
                        width,
                        child.DesiredSize.X);

                height +=
                    child.DesiredSize.Y;
            }
            else
            {
                width +=
                    child.DesiredSize.X;

                height =
                    MathF.Max(
                        height,
                        child.DesiredSize.Y);
            }

            childCount++;
        }

        if (childCount > 1)
        {
            var spacing =
                Spacing *
                (childCount - 1);

            if (Orientation ==
                UiOrientation.Vertical)
            {
                height += spacing;
            }
            else
            {
                width += spacing;
            }
        }

        return new Vector2(
            width,
            height);
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var offset = 0.0f;

        foreach (var child in Children)
        {
            if (Orientation ==
                UiOrientation.Vertical)
            {
                var height =
                    child.DesiredSize.Y;

                child.Arrange(
                    new UiRect(
                        finalRect.X,
                        finalRect.Y + offset,
                        finalRect.Width,
                        height));

                offset +=
                    height +
                    Spacing;
            }
            else
            {
                var width =
                    child.DesiredSize.X;

                child.Arrange(
                    new UiRect(
                        finalRect.X + offset,
                        finalRect.Y,
                        width,
                        finalRect.Height));

                offset +=
                    width +
                    Spacing;
            }
        }
    }
}