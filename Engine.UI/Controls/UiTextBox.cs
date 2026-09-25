using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Controls;

public sealed class UiTextBox : UiWidget
{
    private float _cursorTimer;
    private bool _cursorVisible = true;

    public UiTextBox(
        string text = "")
    {
        Text = text;
        Focusable = true;
        ConsumesKeyboardInput = true;
        Width = 240.0f;
        Height = 42.0f;

        Padding =
            new UiThickness(
                10.0f,
                8.0f,
                10.0f,
                8.0f);

        HorizontalAlignment =
            UiHorizontalAlignment.Left;

        VerticalAlignment =
            UiVerticalAlignment.Top;

        Cursor =
    CursorShape.Text;
    }

    public string Text { get; private set; }

    public string Placeholder { get; set; } = "";

    public int MaxLength { get; set; } = 256;

    public UiThickness Padding { get; set; } =
    new(
        10.0f,
        8.0f,
        10.0f,
        8.0f);

    public float FontSize { get; set; } = 16.0f;

    public UiColor Background { get; set; } =
        new(30, 30, 30, 255);

    public UiColor Border { get; set; } =
        new(80, 80, 80, 255);

    public UiColor FocusedBorder { get; set; } =
        new(220, 220, 220, 255);

    public UiColor TextColor { get; set; } =
        UiColor.White;

    public UiColor PlaceholderColor { get; set; } =
        new(140, 140, 140, 255);

    public UiColor CursorColor { get; set; } =
        UiColor.White;

    public event Action<string>? TextChanged;

    public event Action<string>? Submitted;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? 240.0f,
            Height ?? 42.0f);
    }

    protected override void OnUpdate(
        double deltaSeconds)
    {
        _cursorTimer +=
            (float)deltaSeconds;

        if (_cursorTimer >= 0.5f)
        {
            _cursorTimer = 0.0f;
            _cursorVisible = !_cursorVisible;
        }
    }

    protected override void OnTextInput(
        char character)
    {
        if (!Enabled ||
            Text.Length >= MaxLength ||
            char.IsControl(character))
        {
            return;
        }

        Text +=
            character;

        ResetCursor();

        TextChanged?.Invoke(
            Text);
    }

    protected override void OnKeyPressed(
        TextInputKey key)
    {
        if (key != TextInputKey.Backspace ||
            Text.Length == 0)
        {
            return;
        }

        var indices =
            System.Globalization.StringInfo
                .ParseCombiningCharacters(
                    Text);

        if (indices.Length == 0)
        {
            return;
        }

        var lastIndex =
            indices[^1];

        Text =
            Text[..lastIndex];

        ResetCursor();

        TextChanged?.Invoke(
            Text);
    }

    protected override void OnSubmit()
    {
        Submitted?.Invoke(
            Text);
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        context.DrawRectangle(
            Bounds,
            Background,
            filled: true);

        context.DrawRectangle(
            Bounds,
            IsFocused
                ? FocusedBorder
                : Border,
            filled: false,
            layer: 1);

        var content =
            Bounds.Deflate(
                Padding);

        context.PushClip(
            content);

        var displayText =
            Text.Length > 0
                ? Text
                : Placeholder;

        var textColor =
            Text.Length > 0
                ? TextColor
                : PlaceholderColor;

        var textSize =
            context.MeasureText(
                displayText,
                FontSize);

        var textPosition =
            UiTextLayout.CalculatePosition(
                content,
                textSize,
                UiTextHorizontalAlignment.Left,
                UiTextVerticalAlignment.Center);

        context.DrawText(
            displayText,
            textPosition,
            FontSize,
            textColor);

        if (IsFocused &&
            _cursorVisible)
        {
            var actualTextSize =
                context.MeasureText(
                    Text,
                    FontSize);

            var cursorX =
                content.X +
                actualTextSize.X +
                2.0f;

            var cursorHeight =
                FontSize;

            var cursorY =
                content.Y +
                (content.Height -
                 cursorHeight) /
                2.0f;

            context.DrawRectangle(
                new UiRect(
                    cursorX,
                    cursorY,
                    2.0f,
                    cursorHeight),
                CursorColor,
                filled: true,
                layer: 2);
        }

        context.PopClip();
    }
    private void ResetCursor()
    {
        _cursorTimer = 0.0f;
        _cursorVisible = true;
    }
}