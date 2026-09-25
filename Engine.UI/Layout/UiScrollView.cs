using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.UI.Layout;

public class UiScrollView : UiContainer
{
    public UiWidget? Content { get; private set; }

    public Vector2 ScrollOffset { get; private set; }

    public bool HorizontalScrolling { get; set; }

    public bool VerticalScrolling { get; set; } = true;

    public float ScrollSpeed { get; set; } = 32.0f;

    public UiColor? Background { get; set; }

    public void SetContent(
        UiWidget content)
    {
        ArgumentNullException.ThrowIfNull(
            content);

        if (ReferenceEquals(
                Content,
                content))
        {
            return;
        }

        if (content.Parent is not null)
        {
            throw new InvalidOperationException(
                "The content widget already has a parent.");
        }

        if (Content is not null)
        {
            base.RemoveChild(
                Content);
        }

        base.AddChild(
            content);

        Content =
            content;
    }

    public override bool RemoveChild(
        UiWidget child)
    {
        var removed =
            base.RemoveChild(
                child);

        if (removed &&
            ReferenceEquals(
                Content,
                child))
        {
            Content = null;
        }

        return removed;
    }

    public override void ClearChildren()
    {
        base.ClearChildren();

        Content = null;
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        if (Content is null)
        {
            return availableSize;
        }

        var contentAvailable =
            new Vector2(
                HorizontalScrolling
                    ? float.PositiveInfinity
                    : availableSize.X,

                VerticalScrolling
                    ? float.PositiveInfinity
                    : availableSize.Y);

        Content.Measure(
            context,
            contentAvailable);

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        if (Content is null)
        {
            return;
        }

        var contentWidth =
            MathF.Max(
                finalRect.Width,
                Content.DesiredSize.X);

        var contentHeight =
            MathF.Max(
                finalRect.Height,
                Content.DesiredSize.Y);

        var maxScrollX =
            MathF.Max(
                0.0f,
                contentWidth -
                finalRect.Width);

        var maxScrollY =
            MathF.Max(
                0.0f,
                contentHeight -
                finalRect.Height);

        ScrollOffset =
            new Vector2(
                HorizontalScrolling
                    ? Math.Clamp(
                        ScrollOffset.X,
                        0.0f,
                        maxScrollX)
                    : 0.0f,

                VerticalScrolling
                    ? Math.Clamp(
                        ScrollOffset.Y,
                        0.0f,
                        maxScrollY)
                    : 0.0f);

        Content.Arrange(
            new UiRect(
                finalRect.X -
                ScrollOffset.X,

                finalRect.Y -
                ScrollOffset.Y,

                contentWidth,
                contentHeight));
    }

    public override void Render(
        UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        var previousLayer =
            context.LayerOffset;

        context.LayerOffset +=
            ZIndex;

        if (Background.HasValue)
        {
            context.DrawRectangle(
                Bounds,
                Background.Value,
                filled: true);
        }

        context.PushClip(
            Bounds);

        foreach (var child in Children
                     .OrderBy(
                         static child =>
                             child.ZIndex))
        {
            child.Render(
                context);
        }

        context.PopClip();

        context.LayerOffset =
            previousLayer;
    }

    protected override void OnPointerWheel(
        UiPointerEvent pointer)
    {
        if (pointer.Handled ||
            !Bounds.Contains(
                pointer.Position))
        {
            return;
        }

        if (!VerticalScrolling ||
            MathF.Abs(
                pointer.ScrollDelta) <
            0.0001f)
        {
            return;
        }

        ScrollOffset =
            new Vector2(
                ScrollOffset.X,
                ScrollOffset.Y -
                pointer.ScrollDelta *
                ScrollSpeed);

        pointer.Handled =
            true;
    }

    internal override UiWidget? HitTest(
    Vector2 point)
    {
        if (!Visible ||
            !Enabled ||
            !IsHitTestVisible ||
            !Bounds.Contains(point))
        {
            return null;
        }

        foreach (var child in Children
                     .OrderByDescending(
                         static child =>
                             child.ZIndex))
        {
            var hit =
                child.HitTest(point);

            if (hit is not null)
            {
                return hit;
            }
        }

        return this;
    }
}