using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input.Cursors;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiWindow : UiContainer
{
    private readonly UiOverlayLayer _overlays;

    private readonly UiPanel _header;
    private readonly UiButton _closeButton;
    private readonly UiPanel _contentHost;

    private readonly ResizeHandle _resizeTop;
    private readonly ResizeHandle _resizeBottom;
    private readonly ResizeHandle _resizeLeft;
    private readonly ResizeHandle _resizeRight;
    private readonly ResizeHandle _resizeTopLeft;
    private readonly ResizeHandle _resizeTopRight;
    private readonly ResizeHandle _resizeBottomLeft;
    private readonly ResizeHandle _resizeBottomRight;

    private bool _dragging;
    private Vector2 _dragOffset;

    private bool _resizing;
    private ResizeDirection _resizeDirection;
    private Vector2 _resizeStartPointer;
    private UiRect _resizeStartBounds;

    public UiWindow(
        UiOverlayLayer overlays,
        string title)
    {
        ArgumentNullException.ThrowIfNull(
            overlays);

        ArgumentNullException.ThrowIfNull(
            title);

        _overlays =
            overlays;

        Title =
            title;

        Width = 480.0f;
        Height = 320.0f;

        MinWidth = 320.0f;
        MinHeight = 220.0f;

        Resizable = true;
        Focusable = true;

        _header =
            new UiPanel
            {
                Height = 36.0f,

                Padding =
                    new UiThickness(
                        10.0f,
                        5.0f,
                        5.0f,
                        5.0f),

                Background =
                    new UiColor(
                        35,
                        35,
                        35,
                        255),

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top,

                IsHitTestVisible = true
            };

        _header.AddChild(
            new UiLabel(title)
            {
                FontSize = 16.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Left,

                VerticalAlignment =
                    UiVerticalAlignment.Center
            });

        _closeButton =
            new UiButton("X")
            {
                Width = 28.0f,
                Height = 26.0f,
                FontSize = 14.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Right,

                VerticalAlignment =
                    UiVerticalAlignment.Center
            };

        _closeButton.Clicked +=
            Close;

        _header.AddChild(
            _closeButton);

        _contentHost =
            new UiPanel
            {
                Padding =
                    new UiThickness(
                        10.0f),

                Background =
                    UiColor.Transparent,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        AddChild(
            _header);

        AddChild(
            _contentHost);

        _resizeTop =
            CreateResizeHandle(
                ResizeDirection.Top);

        _resizeBottom =
            CreateResizeHandle(
                ResizeDirection.Bottom);

        _resizeLeft =
            CreateResizeHandle(
                ResizeDirection.Left);

        _resizeRight =
            CreateResizeHandle(
                ResizeDirection.Right);

        _resizeTopLeft =
            CreateResizeHandle(
                ResizeDirection.Top |
                ResizeDirection.Left);

        _resizeTopRight =
            CreateResizeHandle(
                ResizeDirection.Top |
                ResizeDirection.Right);

        _resizeBottomLeft =
            CreateResizeHandle(
                ResizeDirection.Bottom |
                ResizeDirection.Left);

        _resizeBottomRight =
            CreateResizeHandle(
                ResizeDirection.Bottom |
                ResizeDirection.Right);
    }

    public string Title { get; set; }

    public float HeaderHeight =>
        _header.Height ?? 36.0f;

    public UiWidget? Content =>
        _contentHost.Children.Count > 0
            ? _contentHost.Children[0]
            : null;

    public bool Resizable { get; set; }

    public float MinWidth { get; set; }

    public float MinHeight { get; set; }

    public event Action? Closed;

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

        _contentHost.ClearChildren();

        _contentHost.AddChild(
            content);
    }

    public void Show(
        Vector2 position)
    {
        if (Parent is null)
        {
            _overlays.Show(
                this,
                position);
        }
        else
        {
            _overlays.SetPosition(
                this,
                position);
        }

        Visible = true;
    }

    public void Close()
    {
        if (Parent is not null)
        {
            _overlays.Hide(
                this);
        }

        _dragging = false;
        _resizing = false;

        Closed?.Invoke();
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var width =
            Width ?? 480.0f;

        var height =
            Height ?? 320.0f;

        _header.Measure(
            context,
            new Vector2(
                width,
                HeaderHeight));

        _contentHost.Measure(
            context,
            new Vector2(
                width,
                MathF.Max(
                    0.0f,
                    height -
                    HeaderHeight)));

        return new Vector2(
            width,
            height);
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var headerHeight =
            HeaderHeight;

        _header.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y,
                finalRect.Width,
                headerHeight));

        _closeButton.Arrange(
            new UiRect(
                finalRect.Right -
                34.0f,
                finalRect.Y +
                5.0f,
                28.0f,
                26.0f));

        _contentHost.Arrange(
            new UiRect(
                finalRect.X,
                finalRect.Y +
                headerHeight,
                finalRect.Width,
                MathF.Max(
                    0.0f,
                    finalRect.Height -
                    headerHeight)));

        ArrangeResizeHandles(
            finalRect);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        context.DrawRectangle(
            Bounds,
            new UiColor(
                20,
                20,
                20,
                250),
            filled: true);

        context.DrawRectangle(
            Bounds,
            new UiColor(
                100,
                100,
                100,
                255),
            filled: false,
            layer: 1);
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (_resizing)
        {
            return;
        }

        if (pointer.Position.Y >
            Bounds.Y +
            HeaderHeight)
        {
            return;
        }

        _dragging =
            true;

        _dragOffset =
            pointer.Position -
            Bounds.Position;

        pointer.RequestCapture();
        pointer.Handled = true;
    }

    protected override void OnPointerMove(
        UiPointerEvent pointer)
    {
        if (!_dragging)
        {
            return;
        }

        var position =
            pointer.Position -
            _dragOffset;

        _overlays.SetPosition(
            this,
            position);

        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;

        pointer.ReleaseCapture();
        pointer.Handled = true;
    }

    private ResizeHandle CreateResizeHandle(
        ResizeDirection direction)
    {
        var handle =
            new ResizeHandle(
                this,
                direction);

        AddChild(
            handle);

        return handle;
    }

    private void ArrangeResizeHandles(
        UiRect bounds)
    {
        if (!Resizable)
        {
            return;
        }

        const float edgeSize = 8.0f;
        const float cornerSize = 12.0f;

        _resizeTop.Arrange(
            new UiRect(
                bounds.X + cornerSize,
                bounds.Y,
                MathF.Max(
                    0.0f,
                    bounds.Width -
                    cornerSize * 2.0f),
                edgeSize));

        _resizeBottom.Arrange(
            new UiRect(
                bounds.X + cornerSize,
                bounds.Bottom -
                edgeSize,
                MathF.Max(
                    0.0f,
                    bounds.Width -
                    cornerSize * 2.0f),
                edgeSize));

        _resizeLeft.Arrange(
            new UiRect(
                bounds.X,
                bounds.Y + cornerSize,
                edgeSize,
                MathF.Max(
                    0.0f,
                    bounds.Height -
                    cornerSize * 2.0f)));

        _resizeRight.Arrange(
            new UiRect(
                bounds.Right -
                edgeSize,
                bounds.Y + cornerSize,
                edgeSize,
                MathF.Max(
                    0.0f,
                    bounds.Height -
                    cornerSize * 2.0f)));

        _resizeTopLeft.Arrange(
            new UiRect(
                bounds.X,
                bounds.Y,
                cornerSize,
                cornerSize));

        _resizeTopRight.Arrange(
            new UiRect(
                bounds.Right -
                cornerSize,
                bounds.Y,
                cornerSize,
                cornerSize));

        _resizeBottomLeft.Arrange(
            new UiRect(
                bounds.X,
                bounds.Bottom -
                cornerSize,
                cornerSize,
                cornerSize));

        _resizeBottomRight.Arrange(
            new UiRect(
                bounds.Right -
                cornerSize,
                bounds.Bottom -
                cornerSize,
                cornerSize,
                cornerSize));
    }

    private void BeginResize(
        ResizeDirection direction,
        Vector2 pointerPosition)
    {
        if (!Resizable)
        {
            return;
        }

        _resizing = true;
        _dragging = false;

        _resizeDirection =
            direction;

        _resizeStartPointer =
            pointerPosition;

        _resizeStartBounds =
            Bounds;
    }

    private void UpdateResize(
        Vector2 pointerPosition)
    {
        if (!_resizing)
        {
            return;
        }

        var delta =
            pointerPosition -
            _resizeStartPointer;

        var start =
            _resizeStartBounds;

        var x =
            start.X;

        var y =
            start.Y;

        var width =
            start.Width;

        var height =
            start.Height;

        if ((_resizeDirection &
             ResizeDirection.Left) !=
            0)
        {
            var requestedWidth =
                start.Width -
                delta.X;

            width =
                MathF.Max(
                    MinWidth,
                    requestedWidth);

            x =
                start.Right -
                width;
        }

        if ((_resizeDirection &
             ResizeDirection.Right) !=
            0)
        {
            width =
                MathF.Max(
                    MinWidth,
                    start.Width +
                    delta.X);
        }

        if ((_resizeDirection &
             ResizeDirection.Top) !=
            0)
        {
            var requestedHeight =
                start.Height -
                delta.Y;

            height =
                MathF.Max(
                    MinHeight,
                    requestedHeight);

            y =
                start.Bottom -
                height;
        }

        if ((_resizeDirection &
             ResizeDirection.Bottom) !=
            0)
        {
            height =
                MathF.Max(
                    MinHeight,
                    start.Height +
                    delta.Y);
        }

        Width =
            width;

        Height =
            height;

        _overlays.SetPosition(
            this,
            new Vector2(
                x,
                y));
    }

    private void EndResize()
    {
        _resizing = false;
    }

    private sealed class ResizeHandle :
        UiWidget
    {
        private readonly UiWindow _owner;
        private readonly ResizeDirection _direction;

        public ResizeHandle(
            UiWindow owner,
            ResizeDirection direction)
        {
            _owner =
                owner;

            _direction =
                direction;

            IsHitTestVisible =
                true;

            ZIndex =
                1000;

            Cursor =
                direction switch
                {
                    ResizeDirection.Top =>
                        CursorShape.ResizeVertical,

                    ResizeDirection.Bottom =>
                        CursorShape.ResizeVertical,

                    ResizeDirection.Left =>
                        CursorShape.ResizeHorizontal,

                    ResizeDirection.Right =>
                        CursorShape.ResizeHorizontal,

                    ResizeDirection.Top |
                        ResizeDirection.Left =>
                        CursorShape.ResizeDiagonal,

                    ResizeDirection.Bottom |
                        ResizeDirection.Right =>
                        CursorShape.ResizeDiagonal,

                    ResizeDirection.Top |
                        ResizeDirection.Right =>
                        CursorShape.ResizeDiagonalReverse,

                    ResizeDirection.Bottom |
                        ResizeDirection.Left =>
                        CursorShape.ResizeDiagonalReverse,

                    _ =>
                        CursorShape.Default
                };
        }

        protected override void OnPointerDown(
            UiPointerEvent pointer)
        {
            _owner.BeginResize(
                _direction,
                pointer.Position);

            pointer.RequestCapture();
            pointer.Handled = true;
        }

        protected override void OnPointerMove(
            UiPointerEvent pointer)
        {
            _owner.UpdateResize(
                pointer.Position);

            pointer.Handled = true;
        }

        protected override void OnPointerUp(
            UiPointerEvent pointer)
        {
            _owner.EndResize();

            pointer.ReleaseCapture();
            pointer.Handled = true;
        }
    }

    [Flags]
    private enum ResizeDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8
    }
}