using Engine.Core.Math;
using Engine.Input;
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
    private bool _rightPressed;
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
    public event Action<UiPointerEvent>? RightClicked;
    public bool HandleRightClick { get; set; } = true;
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

        var deflatedBounds =
            Bounds.Deflate(Padding);

        // На очень узких кнопках область содержимого
        // не должна иметь отрицательные размеры.
        var contentBounds = new UiRect(
            deflatedBounds.X,
            deflatedBounds.Y,
            MathF.Max(0.0f, deflatedBounds.Width),
            MathF.Max(0.0f, deflatedBounds.Height));

        var displayedText =
            UiTextLayout.TruncateToFit(
                Text,
                contentBounds.Width,
                candidate =>
                    context.MeasureText(
                        candidate,
                        FontSize).X);

        var textSize =
            context.MeasureText(
                displayedText,
                FontSize);

        var textPosition =
            UiTextLayout.CalculatePosition(
                contentBounds,
                textSize,
                HorizontalTextAlignment,
                VerticalTextAlignment);

        // Дополнительная защита от выхода текста
        // за внутренние границы кнопки.
        context.PushClip(contentBounds);

        try
        {
            context.DrawText(
                displayedText,
                textPosition,
                FontSize,
                context.Theme.Text);
        }
        finally
        {
            context.PopClip();
        }
    }


    protected override void OnPointerDown(
    UiPointerEvent pointer)
    {
        if (!Enabled)
        {
            return;
        }

        if (pointer.Button == InputMouseButton.Right)
        {
            // Если событие не подписано, позволяем
            // обработать ПКМ родительскому виджету.
            if (!HandleRightClick ||
                RightClicked is null)
            {
                return;
            }

            _rightPressed = true;
            pointer.Handled = true;
            return;
        }

        if (pointer.Button != InputMouseButton.Left)
        {
            return;
        }

        IsPressed = true;
        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (pointer.Button == InputMouseButton.Right)
        {
            if (!HandleRightClick ||
                !_rightPressed)
            {
                return;
            }

            _rightPressed = false;

            if (ContainsPoint(pointer.Position))
            {
                RightClicked?.Invoke(pointer);
            }

            pointer.Handled = true;
            return;
        }

        if (pointer.Button != InputMouseButton.Left ||
            !IsPressed)
        {
            return;
        }

        IsPressed = false;

        if (ContainsPoint(pointer.Position))
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