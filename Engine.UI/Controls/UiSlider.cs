using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiSlider : UiWidget
{
    private float _value;

    public UiSlider()
    {
        Width = 240.0f;
        Height = 24.0f;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;
    }

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

    public UiColor TrackColor { get; set; } =
        new(45, 45, 45, 255);

    public UiColor FillColor { get; set; } =
        new(90, 160, 220, 255);

    public UiColor KnobColor { get; set; } =
        new(220, 220, 220, 255);

    public float KnobWidth { get; set; } = 12.0f;

    public event Action<float>? ValueChanged;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? 240.0f,
            Height ?? 24.0f);
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        UpdateValue(
            pointer.Position.X);

        IsPressed = true;
    }

    protected override void OnPointerMove(
        UiPointerEvent pointer)
    {
        if (!IsPressed)
        {
            return;
        }

        UpdateValue(
            pointer.Position.X);
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        UpdateValue(
            pointer.Position.X);

        IsPressed = false;
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        var trackHeight =
            6.0f;

        var trackY =
            Bounds.Y +
            (Bounds.Height -
             trackHeight) /
            2.0f;

        var track =
            new UiRect(
                Bounds.X,
                trackY,
                Bounds.Width,
                trackHeight);

        context.DrawRectangle(
            track,
            TrackColor,
            filled: true);

        var fillWidth =
            Bounds.Width *
            Value;

        if (fillWidth > 0.0f)
        {
            context.DrawRectangle(
                new UiRect(
                    Bounds.X,
                    trackY,
                    fillWidth,
                    trackHeight),
                FillColor,
                filled: true,
                layer: 1);
        }

        var knobX =
            Bounds.X +
            Bounds.Width *
            Value -
            KnobWidth /
            2.0f;

        context.DrawRectangle(
            new UiRect(
                knobX,
                Bounds.Y,
                KnobWidth,
                Bounds.Height),
            KnobColor,
            filled: true,
            layer: 2);
    }

    private void UpdateValue(
        float pointerX)
    {
        if (Bounds.Width <= 0.0f)
        {
            return;
        }

        var value =
            (pointerX -
             Bounds.X) /
            Bounds.Width;

        Value =
            Math.Clamp(
                value,
                0.0f,
                1.0f);
    }
}