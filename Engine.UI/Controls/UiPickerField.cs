using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiPickerField : UiWidget
{
    private const string ActionIndicator = "...";

    public UiPickerField(
        string? selectedText = null)
    {
        SelectedText = selectedText;

        Focusable = true;

        Width = 240.0f;
        Height = 42.0f;

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;

        Cursor = CursorShape.Hand;
    }

    public string? SelectedText { get; set; }

    public string Placeholder { get; set; } =
        "Select...";

    public float FontSize { get; set; } = 16.0f;

    public UiThickness Padding { get; set; } =
        new(
            12.0f,
            8.0f,
            12.0f,
            8.0f);

    public event Action? SelectionRequested;

    public void RequestSelection()
    {
        if (!Enabled)
        {
            return;
        }

        SelectionRequested?.Invoke();
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? 240.0f,
            Height ?? 42.0f);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        var background =
            !Enabled
                ? context.Theme.ButtonDisabled
                : IsPressed
                    ? context.Theme.ButtonPressed
                    : IsFocused
                        ? context.Theme.ButtonNormal
                        : IsHovered
                            ? context.Theme.ButtonHovered
                            : context.Theme.ButtonNormal;

        context.DrawRectangle(
            Bounds,
            background,
            filled: true);

        if (Enabled &&
            IsFocused &&
            !IsPressed)
        {
            context.DrawRectangle(
                Bounds,
                context.Theme.ButtonFocusedBorder,
                filled: false,
                layer: 1);
        }

        var contentBounds =
            Bounds.Deflate(Padding);

        var displayText =
            string.IsNullOrEmpty(SelectedText)
                ? Placeholder
                : SelectedText;

        var textColor =
            string.IsNullOrEmpty(SelectedText)
                ? new UiColor(140, 140, 140, 255)
                : context.Theme.Text;

        var indicatorSize =
            context.MeasureText(
                ActionIndicator,
                FontSize);

        var textBounds =
            new UiRect(
                contentBounds.X,
                contentBounds.Y,
                MathF.Max(
                    0.0f,
                    contentBounds.Width -
                    indicatorSize.X -
                    8.0f),
                contentBounds.Height);

        context.PushClip(textBounds);

        var textSize =
            context.MeasureText(
                displayText,
                FontSize);

        var textPosition =
            UiTextLayout.CalculatePosition(
                textBounds,
                textSize,
                UiTextHorizontalAlignment.Left,
                UiTextVerticalAlignment.Center);

        context.DrawText(
            displayText,
            textPosition,
            FontSize,
            textColor);

        context.PopClip();

        var indicatorPosition =
            UiTextLayout.CalculatePosition(
                contentBounds,
                indicatorSize,
                UiTextHorizontalAlignment.Right,
                UiTextVerticalAlignment.Center);

        context.DrawText(
            ActionIndicator,
            indicatorPosition,
            FontSize,
            context.Theme.Text);
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (!Enabled ||
            pointer.Button != InputMouseButton.Left)
        {
            return;
        }

        IsPressed = true;
        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (pointer.Button != InputMouseButton.Left ||
            !IsPressed)
        {
            return;
        }

        IsPressed = false;

        if (ContainsPoint(pointer.Position))
        {
            RequestSelection();
        }

        pointer.Handled = true;
    }

    protected override void OnSubmit()
    {
        RequestSelection();
    }
}