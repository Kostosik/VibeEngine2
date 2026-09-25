using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input.Cursors;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public enum UiButtonState
{
    Normal,
    Hovered,
    Pressed,
    Disabled,
    Focused
}

public sealed class UiButton : UiWidget
{
    public UiButton(
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        Focusable = true;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;

        Cursor =
    CursorShape.Hand;
    }

    public UiTextHorizontalAlignment
    HorizontalTextAlignment
    { get; set; } =
        UiTextHorizontalAlignment.Center;

    public UiTextVerticalAlignment
        VerticalTextAlignment
    { get; set; } =
            UiTextVerticalAlignment.Center;

    public string Text { get; set; }

    public float FontSize { get; set; } = 16.0f;

    public UiThickness Padding { get; set; } =
        new(
            12.0f,
            8.0f,
            12.0f,
            8.0f);

    public UiButtonState State
    {
        get
        {
            if (!Enabled)
            {
                return UiButtonState.Disabled;
            }

            if (IsPressed)
            {
                return UiButtonState.Pressed;
            }

            if (IsFocused)
            {
                return UiButtonState.Focused;
            }

            if (IsHovered)
            {
                return UiButtonState.Hovered;
            }

            return UiButtonState.Normal;
        }
    }

    public event Action? Clicked;

    public void Click()
    {
        if (!Enabled)
        {
            return;
        }

        Clicked?.Invoke();
    }
    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? 180.0f,
            Height ?? 42.0f);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        var background =
            State switch
            {
                UiButtonState.Hovered =>
                    context.Theme.ButtonHovered,

                UiButtonState.Pressed =>
                    context.Theme.ButtonPressed,

                UiButtonState.Disabled =>
                    context.Theme.ButtonDisabled,

                _ =>
                    context.Theme.ButtonNormal
            };

        context.DrawRectangle(
            Bounds,
            background,
            filled: true);

        if (State == UiButtonState.Focused)
        {
            context.DrawRectangle(
                Bounds,
                context.Theme.ButtonFocusedBorder,
                filled: false,
                layer: 1);
        }

        var contentBounds =
            Bounds.Deflate(
                Padding);

        var textSize =
            context.MeasureText(
                Text,
                FontSize);

        var textPosition =
            UiTextLayout.CalculatePosition(
                contentBounds,
                textSize,
                HorizontalTextAlignment,
                VerticalTextAlignment);

        context.DrawText(
            Text,
            textPosition,
            FontSize,
            context.Theme.Text);
    }

    protected override void OnPointerDown(
    UiPointerEvent pointer)
    {
        if (!Enabled)
        {
            return;
        }

        IsPressed = true;
        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (!IsPressed)
        {
            return;
        }

        IsPressed = false;

        if (Bounds.Contains(
                pointer.Position))
        {
            Click();
        }

        pointer.Handled = true;
    }

    protected override void OnSubmit()
    {
        Click();
    }
}