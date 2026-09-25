using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.UI.Core;

public sealed class UiOverlayLayer : UiCanvas
{
    public UiOverlayLayer()
    {
        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;

        VerticalAlignment =
            UiVerticalAlignment.Stretch;

        IsHitTestVisible = true;

        ZIndex = 10000;
    }

    public void Show(
        UiWidget widget,
        Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(widget);

        if (widget.Parent is not null)
        {
            throw new InvalidOperationException(
                "The widget already has a parent.");
        }

        AddChild(widget);

        SetPosition(
            widget,
            position);

        widget.Visible = true;
    }

    public void Hide(
        UiWidget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);

        RemoveChild(widget);
    }

    internal override UiWidget? HitTest(
        Vector2 point)
    {
        if (!Visible ||
            !Enabled ||
            !IsHitTestVisible)
        {
            return null;
        }

        var children =
            Children
                .Select(
                    static (child, index) =>
                        (child, index))
                .OrderByDescending(
                    static item => item.child.ZIndex)
                .ThenByDescending(
                    static item => item.index);

        foreach (var item in children)
        {
            var hit =
                item.child.HitTest(
                    point);

            if (hit is not null)
            {
                return hit;
            }
        }

        return null;
    }
}