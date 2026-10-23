using Engine.Core.Math;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Core;

public abstract class UiWidget
{
    public UiContainer? Parent { get; internal set; }

    public bool Visible { get; set; } = true;

    public bool Enabled { get; set; } = true;
    public float RotationDegrees { get; set; }
    public bool Focusable { get; set; }
    public bool ConsumesKeyboardInput { get; protected set; }
    public bool IsHitTestVisible { get; set; } = true;

    public bool IsHovered { get; internal set; }

    public bool IsPressed { get; set; }

    public bool IsFocused { get; internal set; }

    public CursorShape Cursor { get; set; } =
    CursorShape.Default;

    public int ZIndex { get; set; }

    public UiThickness Margin { get; set; } =
        UiThickness.Zero;

    public UiHorizontalAlignment HorizontalAlignment
    {
        get;
        set;
    } = UiHorizontalAlignment.Stretch;

    public UiVerticalAlignment VerticalAlignment
    {
        get;
        set;
    } = UiVerticalAlignment.Stretch;

    public float? Width { get; set; }

    public float? Height { get; set; }

    internal UiFocusManager? ResolveFocusManager()
    {
        var current =
            this;

        while (current.Parent is not null)
        {
            current =
                current.Parent;
        }

        return current is UiRoot root
            ? root.FocusManager
            : null;
    }

    public UiLayoutMode LayoutMode { get; set; } =
    UiLayoutMode.Alignment;

    public UiAnchor Anchor { get; set; } =
        UiAnchor.TopLeft;

    public Vector2 Offset { get; set; } =
        Vector2.Zero;
    public Vector2 DesiredSize { get; private set; }

    public UiRect Bounds { get; private set; }

    internal void RaisePointerWheel(
        UiPointerEvent pointer)
    {
        OnPointerWheel(pointer);
    }

    protected virtual void OnPointerWheel(
        UiPointerEvent pointer)
    {
    }

    internal void NotifyDetachingFromTree()
    {
        OnDetachingFromTree();

        if (this is not UiContainer container)
        {
            return;
        }

        foreach (var child in container.Children.ToArray())
        {
            child.NotifyDetachingFromTree();
        }
    }

    protected virtual void OnDetachingFromTree()
    {
    }

    public void Measure(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        ArgumentNullException.ThrowIfNull(context);

        var availableWidth =
            MathF.Max(
                0.0f,
                availableSize.X -
                Margin.Left -
                Margin.Right);

        var availableHeight =
            MathF.Max(
                0.0f,
                availableSize.Y -
                Margin.Top -
                Margin.Bottom);

        var contentSize =
            MeasureCore(
                context,
                new Vector2(
                    availableWidth,
                    availableHeight));

        var width =
            Width ??
            contentSize.X;

        var height =
            Height ??
            contentSize.Y;

        DesiredSize =
            new Vector2(
                width +
                Margin.Left +
                Margin.Right,
                height +
                Margin.Top +
                Margin.Bottom);
    }

    public void Arrange(
    UiRect finalRect)
    {
        var available =
            finalRect.Deflate(
                Margin);

        var desiredWidth =
            MathF.Max(
                0.0f,
                DesiredSize.X -
                Margin.Left -
                Margin.Right);

        var desiredHeight =
            MathF.Max(
                0.0f,
                DesiredSize.Y -
                Margin.Top -
                Margin.Bottom);

        if (LayoutMode ==
    UiLayoutMode.Anchor)
        {
            var anchorWidth =
                MathF.Max(
                    0.0f,
                    Width ??
                    desiredWidth);

            var anchorHeight =
                MathF.Max(
                    0.0f,
                    Height ??
                    desiredHeight);

            var anchorX =
                available.X +
                available.Width *
                Anchor.X +
                Offset.X -
                anchorWidth *
                Anchor.X;

            var anchorY =
                available.Y +
                available.Height *
                Anchor.Y +
                Offset.Y -
                anchorHeight *
                Anchor.Y;

            Bounds =
                new UiRect(
                    anchorX,
                    anchorY,
                    anchorWidth,
                    anchorHeight);

            ArrangeCore(
                Bounds);

            return;
        }

        var width =
            Width ??
            (HorizontalAlignment ==
             UiHorizontalAlignment.Stretch
                ? available.Width
                : desiredWidth);

        var height =
            Height ??
            (VerticalAlignment ==
             UiVerticalAlignment.Stretch
                ? available.Height
                : desiredHeight);

        width =
            MathF.Min(
                MathF.Max(
                    0.0f,
                    width),
                available.Width);

        height =
            MathF.Min(
                MathF.Max(
                    0.0f,
                    height),
                available.Height);

        var x =
            HorizontalAlignment switch
            {
                UiHorizontalAlignment.Center =>
                    available.X +
                    (available.Width - width) /
                    2.0f,

                UiHorizontalAlignment.Right =>
                    available.Right -
                    width,

                _ =>
                    available.X
            };

        var y =
            VerticalAlignment switch
            {
                UiVerticalAlignment.Center =>
                    available.Y +
                    (available.Height - height) /
                    2.0f,

                UiVerticalAlignment.Bottom =>
                    available.Bottom -
                    height,

                _ =>
                    available.Y
            };

        Bounds =
            new UiRect(
                x,
                y,
                width,
                height);

        ArrangeCore(
            Bounds);
    }

    public virtual void Update(
        double deltaSeconds)
    {
        if (!Visible ||
            !Enabled)
        {
            return;
        }

        OnUpdate(
            deltaSeconds);
    }

    public virtual void Render(
        UiRenderContext context)
    {
        if (!Visible)
        {
            return;
        }

        context.PushRotation(
            new Vector2(
                Bounds.X + Bounds.Width * 0.5f,
                Bounds.Y + Bounds.Height * 0.5f),
            RotationDegrees);

        try
        {
            OnRender(context);
        }
        finally
        {
            context.PopRotation();
        }
    }

    protected virtual Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return Vector2.Zero;
    }

    protected virtual void ArrangeCore(
        UiRect finalRect)
    {
    }

    protected virtual void OnUpdate(
        double deltaSeconds)
    {
    }

    protected virtual void OnRender(
        UiRenderContext context)
    {
    }

    protected Vector2 TransformPointToLocalSpace(
        Vector2 point)
    {
        var ancestors = new Stack<UiWidget>();

        for (UiWidget? current = this;
             current is not null;
             current = current.Parent)
        {
            ancestors.Push(current);
        }

        // Стек выдаёт предков от корня к текущему виджету.
        // Именно в таком порядке отменяем повороты.
        while (ancestors.Count > 0)
        {
            point = ancestors.Pop().TransformPointToLocal(point);
        }

        return point;
    }

    private Vector2 TransformPointToLocal(
        Vector2 point)
    {
        if (MathF.Abs(RotationDegrees) < 0.0001f)
        {
            return point;
        }

        var center = new Vector2(
            Bounds.X + Bounds.Width * 0.5f,
            Bounds.Y + Bounds.Height * 0.5f);

        var radians =
            -RotationDegrees * (MathF.PI / 180.0f);

        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - center.X;
        var dy = point.Y - center.Y;

        return new Vector2(
            center.X + dx * cos - dy * sin,
            center.Y + dx * sin + dy * cos);
    }

    protected bool ContainsPoint(Vector2 point)
    {
        return Bounds.Contains(
            TransformPointToLocalSpace(point));
    }



    internal virtual UiWidget? HitTest(
        Vector2 point)
    {
        if (!Visible ||
            !Enabled ||
            !IsHitTestVisible)
        {
            return null;
        }

        return ContainsPoint(point)
            ? this
            : null;
    }

    internal void RaisePointerEnter(
        UiPointerEvent pointer)
    {
        OnPointerEnter(pointer);
    }

    internal void RaisePointerLeave(
        UiPointerEvent pointer)
    {
        OnPointerLeave(pointer);
    }

    internal void RaisePointerMove(
        UiPointerEvent pointer)
    {
        OnPointerMove(pointer);
    }

    internal void RaisePointerDown(
        UiPointerEvent pointer)
    {
        OnPointerDown(pointer);
    }

    internal void RaisePointerUp(
        UiPointerEvent pointer)
    {
        OnPointerUp(pointer);
    }

    internal void RaiseTextInput(
        char character)
    {
        OnTextInput(
            character);
    }

    internal void RaiseKeyPressed(
        TextInputKey key)
    {
        OnKeyPressed(
            key);
    }

    internal void RaiseKeyEvent(
    UiKeyEvent keyEvent)
    {
        OnKeyEvent(
            keyEvent);
    }

    internal void RaiseSubmit()
    {
        OnSubmit();
    }

    protected virtual void OnPointerEnter(
        UiPointerEvent pointer)
    {
    }

    protected virtual void OnPointerLeave(
        UiPointerEvent pointer)
    {
    }

    protected virtual void OnPointerMove(
        UiPointerEvent pointer)
    {
    }

    protected virtual void OnPointerDown(
        UiPointerEvent pointer)
    {
    }

    protected virtual void OnPointerUp(
        UiPointerEvent pointer)
    {
    }

    protected virtual void OnTextInput(
        char character)
    {
    }

    protected virtual void OnKeyPressed(
        TextInputKey key)
    {
    }

    protected virtual void OnKeyEvent(
    UiKeyEvent keyEvent)
    {
    }
    protected virtual void OnSubmit()
    {
    }
}