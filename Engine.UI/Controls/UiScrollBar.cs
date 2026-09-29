using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input.Cursors;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public enum UiScrollBarOrientation
{
    Vertical,
    Horizontal
}

public sealed class UiScrollBar : UiWidget
{
    private float _value;
    private float _thumbDragOffset;

    public UiScrollBar(
        UiScrollBarOrientation orientation)
    {
        Orientation =
            orientation;

        Width =
            orientation ==
            UiScrollBarOrientation.Vertical
                ? 14.0f
                : null;

        Height =
            orientation ==
            UiScrollBarOrientation.Horizontal
                ? 14.0f
                : null;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;

        Cursor =
            orientation ==
            UiScrollBarOrientation.Vertical
                ? CursorShape.ResizeVertical
                : CursorShape.ResizeHorizontal;
    }

    public UiScrollBarOrientation Orientation { get; }

    public float Value
    {
        get => _value;
        set
        {
            var newValue =
                Math.Clamp(
                    value,
                    0.0f,
                    1.0f);

            if (MathF.Abs(
                    _value - newValue) <
                0.0001f)
            {
                return;
            }

            _value =
                newValue;

            ValueChanged?.Invoke(
                _value);
        }
    }

    public float ViewportRatio { get; set; } = 1.0f;

    public float MinimumThumbSize { get; set; } = 24.0f;

    public UiColor TrackColor { get; set; } =
        new(35, 35, 35, 180);

    public UiColor ThumbColor { get; set; } =
        new(100, 100, 100, 230);

    public UiColor ThumbHoveredColor { get; set; } =
        new(125, 125, 125, 240);

    public UiColor ThumbPressedColor { get; set; } =
        new(150, 150, 150, 255);

    public event Action<float>? ValueChanged;

    internal void SetValueSilently(
        float value)
    {
        _value =
            Math.Clamp(
                value,
                0.0f,
                1.0f);
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ??
            (Orientation ==
             UiScrollBarOrientation.Vertical
                ? 14.0f
                : availableSize.X),

            Height ??
            (Orientation ==
             UiScrollBarOrientation.Horizontal
                ? 14.0f
                : availableSize.Y));
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (!Enabled)
        {
            return;
        }

        var thumb =
            GetThumbBounds();

        var pointerCoordinate =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? pointer.Position.Y
                : pointer.Position.X;

        var thumbStart =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? thumb.Y
                : thumb.X;

        var thumbLength =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? thumb.Height
                : thumb.Width;

        if (thumb.Contains(
                pointer.Position))
        {
            _thumbDragOffset =
                pointerCoordinate -
                thumbStart;
        }
        else
        {
            _thumbDragOffset =
                thumbLength /
                2.0f;

            SetValueFromPointer(
                pointerCoordinate);
        }

        IsPressed =
            true;

        pointer.Handled =
            true;

        pointer.RequestCapture();
    }

    protected override void OnPointerMove(
        UiPointerEvent pointer)
    {
        if (!IsPressed)
        {
            return;
        }

        var pointerCoordinate =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? pointer.Position.Y
                : pointer.Position.X;

        SetValueFromPointer(
            pointerCoordinate);

        pointer.Handled =
            true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (!IsPressed)
        {
            return;
        }

        var pointerCoordinate =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? pointer.Position.Y
                : pointer.Position.X;

        SetValueFromPointer(
            pointerCoordinate);

        IsPressed =
            false;

        pointer.Handled =
            true;

        pointer.ReleaseCapture();
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        context.DrawRectangle(
            Bounds,
            TrackColor,
            filled: true);

        var thumb =
            GetThumbBounds();

        var thumbColor =
            IsPressed
                ? ThumbPressedColor
                : IsHovered
                    ? ThumbHoveredColor
                    : ThumbColor;

        context.DrawRectangle(
            thumb,
            thumbColor,
            filled: true,
            layer: 1);
    }

    private void SetValueFromPointer(
        float pointerCoordinate)
    {
        var trackStart =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? Bounds.Y
                : Bounds.X;

        var trackLength =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? Bounds.Height
                : Bounds.Width;

        var thumbLength =
            GetThumbLength(
                trackLength);

        var movableLength =
            trackLength -
            thumbLength;

        if (movableLength <= 0.0f)
        {
            Value = 0.0f;
            return;
        }

        var position =
            pointerCoordinate -
            trackStart -
            _thumbDragOffset;

        Value =
            Math.Clamp(
                position /
                movableLength,
                0.0f,
                1.0f);
    }

    private UiRect GetThumbBounds()
    {
        var trackLength =
            Orientation ==
            UiScrollBarOrientation.Vertical
                ? Bounds.Height
                : Bounds.Width;

        var thumbLength =
            GetThumbLength(
                trackLength);

        var movableLength =
            MathF.Max(
                0.0f,
                trackLength -
                thumbLength);

        var position =
            movableLength *
            Value;

        if (Orientation ==
            UiScrollBarOrientation.Vertical)
        {
            return new UiRect(
                Bounds.X,
                Bounds.Y + position,
                Bounds.Width,
                thumbLength);
        }

        return new UiRect(
            Bounds.X + position,
            Bounds.Y,
            thumbLength,
            Bounds.Height);
    }

    private float GetThumbLength(
        float trackLength)
    {
        if (trackLength <= 0.0f)
        {
            return 0.0f;
        }

        var ratio =
            Math.Clamp(
                ViewportRatio,
                0.0f,
                1.0f);

        return Math.Clamp(
            trackLength * ratio,
            MathF.Min(
                MinimumThumbSize,
                trackLength),
            trackLength);
    }
}