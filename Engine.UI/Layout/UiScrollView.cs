using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.UI.Layout;

public class UiScrollView : UiContainer
{
    private readonly UiScrollBar _verticalScrollBar;
    private readonly UiScrollBar _horizontalScrollBar;

    public UiScrollView()
    {
        _verticalScrollBar =
            new UiScrollBar(
                UiScrollBarOrientation.Vertical)
            {
                ZIndex = 100
            };

        _horizontalScrollBar =
            new UiScrollBar(
                UiScrollBarOrientation.Horizontal)
            {
                ZIndex = 100
            };

        _verticalScrollBar.ValueChanged +=
            OnVerticalScrollBarChanged;

        _horizontalScrollBar.ValueChanged +=
            OnHorizontalScrollBarChanged;

        base.AddChild(
            _verticalScrollBar);

        base.AddChild(
            _horizontalScrollBar);
    }

    public UiWidget? Content { get; private set; }

    public Vector2 ScrollOffset { get; private set; }

    public bool HorizontalScrolling { get; set; }

    public bool VerticalScrolling { get; set; } = true;

    public bool ShowHorizontalScrollBar { get; set; } = true;

    public bool ShowVerticalScrollBar { get; set; } = true;

    public float ScrollBarThickness { get; set; } = 14.0f;

    public float ScrollBarMinimumThumbSize { get; set; } = 24.0f;

    public float ScrollSpeed { get; set; } = 32.0f;

    public UiColor? Background { get; set; }

    public void ScrollToBottom()
    {
        if (Content is null ||
            Bounds.Height <= 0.0f)
        {
            return;
        }

        var contentHeight =
            MathF.Max(
                Bounds.Height,
                Content.DesiredSize.Y);

        var maxScrollY =
            MathF.Max(
                0.0f,
                contentHeight - Bounds.Height);

        ScrollOffset =
            new Vector2(
                ScrollOffset.X,
                maxScrollY);

        ArrangeContent();

        _verticalScrollBar.SetValueSilently(
            maxScrollY > 0.0f
                ? 1.0f
                : 0.0f);
    }

    public void SetContent(
        UiWidget content)
    {
        ArgumentNullException.ThrowIfNull(
            content);

        if (ReferenceEquals(
                content,
                this))
        {
            throw new InvalidOperationException(
                "A widget cannot be its own content.");
        }

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
        if (ReferenceEquals(
                child,
                _verticalScrollBar) ||
            ReferenceEquals(
                child,
                _horizontalScrollBar))
        {
            return false;
        }

        var removed =
            base.RemoveChild(
                child);

        if (removed &&
            ReferenceEquals(
                Content,
                child))
        {
            Content = null;

            ScrollOffset =
                Vector2.Zero;
        }

        return removed;
    }

    public override void ClearChildren()
    {
        base.ClearChildren();

        Content = null;

        base.AddChild(
            _verticalScrollBar);

        base.AddChild(
            _horizontalScrollBar);
    }

    protected override Vector2 MeasureCore(
    UiLayoutContext context,
    Vector2 availableSize)
    {
        if (Content is null)
        {
            return new Vector2(
                float.IsFinite(availableSize.X)
                    ? availableSize.X
                    : 0.0f,

                float.IsFinite(availableSize.Y)
                    ? availableSize.Y
                    : 0.0f);
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

        _verticalScrollBar.Measure(
            context,
            availableSize);

        _horizontalScrollBar.Measure(
            context,
            availableSize);

        // В ограниченном пространстве ScrollView занимает
        // доступную область. При бесконечном ограничении
        // возвращаем естественный размер содержимого.
        return new Vector2(
            float.IsFinite(availableSize.X)
                ? availableSize.X
                : Content.DesiredSize.X,

            float.IsFinite(availableSize.Y)
                ? availableSize.Y
                : Content.DesiredSize.Y);
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        if (Content is null)
        {
            _verticalScrollBar.Visible =
                false;

            _horizontalScrollBar.Visible =
                false;

            return;
        }

        var contentWidth = HorizontalScrolling
            ? MathF.Max(
                finalRect.Width,
                Content.DesiredSize.X)
            : finalRect.Width;

        var contentHeight = VerticalScrolling
            ? MathF.Max(
                finalRect.Height,
                Content.DesiredSize.Y)
            : finalRect.Height;

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

        var verticalVisible =
            ShowVerticalScrollBar &&
            VerticalScrolling &&
            maxScrollY > 0.0001f;

        var horizontalVisible =
            ShowHorizontalScrollBar &&
            HorizontalScrolling &&
            maxScrollX > 0.0001f;

        _verticalScrollBar.Visible =
            verticalVisible;

        _horizontalScrollBar.Visible =
            horizontalVisible;

        _verticalScrollBar.ViewportRatio =
            contentHeight <= 0.0f
                ? 1.0f
                : finalRect.Height /
                  contentHeight;

        _horizontalScrollBar.ViewportRatio =
            contentWidth <= 0.0f
                ? 1.0f
                : finalRect.Width /
                  contentWidth;

        _verticalScrollBar.MinimumThumbSize =
            ScrollBarMinimumThumbSize;

        _horizontalScrollBar.MinimumThumbSize =
            ScrollBarMinimumThumbSize;

        _verticalScrollBar.SetValueSilently(
            maxScrollY > 0.0f
                ? ScrollOffset.Y /
                  maxScrollY
                : 0.0f);

        _horizontalScrollBar.SetValueSilently(
            maxScrollX > 0.0f
                ? ScrollOffset.X /
                  maxScrollX
                : 0.0f);

        var verticalWidth =
            MathF.Min(
                ScrollBarThickness,
                finalRect.Width);

        var horizontalHeight =
            MathF.Min(
                ScrollBarThickness,
                finalRect.Height);

        _verticalScrollBar.Arrange(
            new UiRect(
                finalRect.Right -
                verticalWidth,

                finalRect.Y,

                verticalWidth,

                MathF.Max(
                    0.0f,
                    finalRect.Height -
                    (horizontalVisible
                        ? horizontalHeight
                        : 0.0f))));

        _horizontalScrollBar.Arrange(
            new UiRect(
                finalRect.X,

                finalRect.Bottom -
                horizontalHeight,

                MathF.Max(
                    0.0f,
                    finalRect.Width -
                    (verticalVisible
                        ? verticalWidth
                        : 0.0f)),

                horizontalHeight));
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

        var rotationCenter =
            new Vector2(
                Bounds.X + Bounds.Width * 0.5f,
                Bounds.Y + Bounds.Height * 0.5f);

        var rotationPushed = false;
        var clipPushed = false;

        try
        {
            context.LayerOffset += ZIndex;

            context.PushRotation(
                rotationCenter,
                RotationDegrees);

            rotationPushed = true;

            if (Background.HasValue)
            {
                context.DrawRectangle(
                    Bounds,
                    Background.Value,
                    filled: true);
            }

            context.PushClip(
                Bounds);

            clipPushed = true;

            foreach (var child in Children
                         .OrderBy(
                             static child =>
                                 child.ZIndex))
            {
                child.Render(context);
            }
        }
        finally
        {
            try
            {
                if (clipPushed)
                {
                    context.PopClip();
                }
            }
            finally
            {
                try
                {
                    if (rotationPushed)
                    {
                        context.PopRotation();
                    }
                }
                finally
                {
                    context.LayerOffset =
                        previousLayer;
                }
            }
        }
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

        var deltaX =
            HorizontalScrolling
                ? -pointer.ScrollDelta *
                  ScrollSpeed
                : 0.0f;

        var deltaY =
            VerticalScrolling
                ? -pointer.ScrollDelta *
                  ScrollSpeed
                : 0.0f;

        if (MathF.Abs(deltaX) <
                0.0001f &&
            MathF.Abs(deltaY) <
                0.0001f)
        {
            return;
        }

        var previousOffset = ScrollOffset;

        SetScrollOffset(
            new Vector2(
                ScrollOffset.X + deltaX,
                ScrollOffset.Y + deltaY));

        pointer.Handled =
            MathF.Abs(
                ScrollOffset.X - previousOffset.X) > 0.0001f ||
            MathF.Abs(
                ScrollOffset.Y - previousOffset.Y) > 0.0001f;
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
                     .Reverse()
                     .OrderByDescending(
                         static child =>
                             child.ZIndex))
        {
            var hit =
                child.HitTest(
                    point);

            if (hit is not null)
            {
                return hit;
            }
        }

        return this;
    }

    private void OnVerticalScrollBarChanged(
        float value)
    {
        if (Content is null ||
            Bounds.Height <= 0.0f)
        {
            return;
        }

        var contentHeight =
            MathF.Max(
                Bounds.Height,
                Content.DesiredSize.Y);

        var maxScrollY =
            MathF.Max(
                0.0f,
                contentHeight -
                Bounds.Height);

        ScrollOffset =
            new Vector2(
                ScrollOffset.X,
                value *
                maxScrollY);

        ArrangeContent();
    }

    private void OnHorizontalScrollBarChanged(
        float value)
    {
        if (Content is null ||
            Bounds.Width <= 0.0f)
        {
            return;
        }

        var contentWidth =
            MathF.Max(
                Bounds.Width,
                Content.DesiredSize.X);

        var maxScrollX =
            MathF.Max(
                0.0f,
                contentWidth -
                Bounds.Width);

        ScrollOffset =
            new Vector2(
                value *
                maxScrollX,
                ScrollOffset.Y);

        ArrangeContent();
    }

    private void SetScrollOffset(
        Vector2 offset)
    {
        if (Content is null)
        {
            ScrollOffset =
                Vector2.Zero;

            return;
        }

        var contentWidth =
            MathF.Max(
                Bounds.Width,
                Content.DesiredSize.X);

        var contentHeight =
            MathF.Max(
                Bounds.Height,
                Content.DesiredSize.Y);

        var maxScrollX =
            MathF.Max(
                0.0f,
                contentWidth -
                Bounds.Width);

        var maxScrollY =
            MathF.Max(
                0.0f,
                contentHeight -
                Bounds.Height);

        ScrollOffset =
            new Vector2(
                HorizontalScrolling
                    ? Math.Clamp(
                        offset.X,
                        0.0f,
                        maxScrollX)
                    : 0.0f,

                VerticalScrolling
                    ? Math.Clamp(
                        offset.Y,
                        0.0f,
                        maxScrollY)
                    : 0.0f);

        ArrangeContent();

        _horizontalScrollBar.SetValueSilently(
            maxScrollX > 0.0f
                ? ScrollOffset.X /
                  maxScrollX
                : 0.0f);

        _verticalScrollBar.SetValueSilently(
            maxScrollY > 0.0f
                ? ScrollOffset.Y /
                  maxScrollY
                : 0.0f);
    }

    private void ArrangeContent()
    {
        if (Content is null)
        {
            return;
        }

        var contentWidth = HorizontalScrolling
            ? MathF.Max(
                Bounds.Width,
                Content.DesiredSize.X)
            : Bounds.Width;

        var contentHeight = VerticalScrolling
            ? MathF.Max(
                Bounds.Height,
                Content.DesiredSize.Y)
            : Bounds.Height;

        Content.Arrange(
            new UiRect(
                Bounds.X -
                ScrollOffset.X,

                Bounds.Y -
                ScrollOffset.Y,

                contentWidth,
                contentHeight));
    }
}