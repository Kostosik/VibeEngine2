using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiToggle : UiWidget
{
    public UiToggle(
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        Focusable = true;

        Width = 220.0f;
        Height = 36.0f;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;
    }

    public bool ShowFocusVisual { get; set; } = true;

    public string Text { get; set; }

    public bool IsOn { get; private set; }

    public float FontSize { get; set; } = 16.0f;

    public float SwitchWidth { get; set; } = 42.0f;

    public float SwitchHeight { get; set; } = 22.0f;

    public UiColor Background { get; set; } =
        new(45, 45, 45, 255);

    public UiColor OnBackground { get; set; } =
        new(70, 150, 90, 255);

    public UiColor Knob { get; set; } =
        new(220, 220, 220, 255);

    public UiColor TextColor { get; set; } =
        UiColor.White;

    public event Action<bool>? ValueChanged;

    public void SetValue(
        bool value)
    {
        if (IsOn == value)
        {
            return;
        }

        IsOn = value;

        ValueChanged?.Invoke(
            IsOn);
    }

    public void Toggle()
    {
        SetValue(!IsOn);
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? 220.0f,
            Height ?? 36.0f);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        var switchX =
            Bounds.X;

        var switchY =
            Bounds.Y +
            (Bounds.Height -
             SwitchHeight) /
            2.0f;

        context.DrawRectangle(
            new UiRect(
                switchX,
                switchY,
                SwitchWidth,
                SwitchHeight),
            IsOn
                ? OnBackground
                : Background,
            filled: true);

        var knobSize =
            SwitchHeight - 4.0f;

        var knobX =
            IsOn
                ? switchX +
                  SwitchWidth -
                  knobSize -
                  2.0f
                : switchX + 2.0f;

        context.DrawRectangle(
            new UiRect(
                knobX,
                switchY + 2.0f,
                knobSize,
                knobSize),
            Knob,
            filled: true,
            layer: 1);

        var textPosition =
            new Vector2(
                switchX +
                SwitchWidth +
                10.0f,
                Bounds.Y);

        var content =
            new UiRect(
                textPosition.X,
                Bounds.Y,
                Bounds.Width -
                SwitchWidth -
                10.0f,
                Bounds.Height);

        var textSize =
            context.MeasureText(
                Text,
                FontSize);

        var alignedPosition =
            UiTextLayout.CalculatePosition(
                content,
                textSize,
                UiTextHorizontalAlignment.Left,
                UiTextVerticalAlignment.Center);

        context.DrawText(
            Text,
            alignedPosition,
            FontSize,
            TextColor);

        if (IsFocused && ShowFocusVisual)
        {
            context.DrawRectangle(
                Bounds,
                context.Theme.ButtonFocusedBorder,
                filled: false,
                layer: 2);
        }
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (Bounds.Contains(
                pointer.Position))
        {
            Toggle();
        }
    }

    protected override void OnSubmit()
    {
        Toggle();
    }
}