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
    private UiLayoutContext? _layoutContext;

    public UiTextBox(
        string text = "")
    {
        Text = text;
        CursorIndex = text.Length;

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

    public int CursorIndex { get; private set; }

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
        _layoutContext =
            context;

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

        Text =
            Text.Insert(
                CursorIndex,
                character.ToString());

        CursorIndex++;

        ResetCursor();

        TextChanged?.Invoke(
            Text);
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (!Enabled)
        {
            return;
        }

        SetCursorFromPointer(
            pointer.Position.X);

        ResetCursor();

        pointer.Handled = true;
    }

    protected override void OnKeyPressed(
        TextInputKey key)
    {
        switch (key)
        {
            case TextInputKey.Backspace:
                DeletePreviousTextElement();
                break;

            case TextInputKey.Delete:
                DeleteNextTextElement();
                break;
        }
    }

    protected override void OnKeyEvent(
        UiKeyEvent keyEvent)
    {
        switch (keyEvent.Key)
        {
            case TextInputKey.Left:
                MoveCursorLeft();
                keyEvent.Handled = true;
                break;

            case TextInputKey.Right:
                MoveCursorRight();
                keyEvent.Handled = true;
                break;

            case TextInputKey.Home:
                CursorIndex = 0;
                ResetCursor();
                keyEvent.Handled = true;
                break;

            case TextInputKey.End:
                CursorIndex = Text.Length;
                ResetCursor();
                keyEvent.Handled = true;
                break;
        }
    }

    private void DeletePreviousTextElement()
    {
        if (CursorIndex == 0)
        {
            return;
        }

        var starts =
            GetTextElementStarts();

        var previousIndex =
            FindPreviousTextElementStart(
                starts,
                CursorIndex);

        if (previousIndex < 0)
        {
            return;
        }

        Text =
            Text.Remove(
                previousIndex,
                CursorIndex - previousIndex);

        CursorIndex =
            previousIndex;

        ResetCursor();

        TextChanged?.Invoke(
            Text);
    }

    private void DeleteNextTextElement()
    {
        if (CursorIndex >= Text.Length)
        {
            return;
        }

        var starts =
            GetTextElementStarts();

        var nextIndex =
            FindNextTextElementStart(
                starts,
                CursorIndex);

        var endIndex =
            nextIndex >= 0
                ? nextIndex
                : Text.Length;

        Text =
            Text.Remove(
                CursorIndex,
                endIndex - CursorIndex);

        ResetCursor();

        TextChanged?.Invoke(
            Text);
    }

    private static int FindNextTextElementStart(
        int[] starts,
        int cursorIndex)
    {
        foreach (var start in starts)
        {
            if (start > cursorIndex)
            {
                return start;
            }
        }

        return -1;
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
            var textBeforeCursor =
                Text[..CursorIndex];

            var actualTextSize =
                context.MeasureText(
                    textBeforeCursor,
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

    private void MoveCursorLeft()
    {
        if (CursorIndex == 0)
        {
            return;
        }

        var starts =
            GetTextElementStarts();

        var previousIndex =
            FindPreviousTextElementStart(
                starts,
                CursorIndex);

        if (previousIndex < 0)
        {
            return;
        }

        CursorIndex =
            previousIndex;

        ResetCursor();
    }

    private void MoveCursorRight()
    {
        if (CursorIndex >= Text.Length)
        {
            return;
        }

        var starts =
            GetTextElementStarts();

        foreach (var start in starts)
        {
            if (start > CursorIndex)
            {
                CursorIndex =
                    start;

                ResetCursor();
                return;
            }
        }

        CursorIndex =
            Text.Length;

        ResetCursor();
    }

    private int[] GetTextElementStarts()
    {
        return System.Globalization.StringInfo
            .ParseCombiningCharacters(
                Text);
    }

    private void SetCursorFromPointer(
    float pointerX)
    {
        if (Text.Length == 0)
        {
            CursorIndex = 0;
            return;
        }

        var content =
            Bounds.Deflate(
                Padding);

        var localX =
            pointerX -
            content.X;

        if (localX <= 0.0f)
        {
            CursorIndex = 0;
            return;
        }

        if (_layoutContext is null)
        {
            CursorIndex = Text.Length;
            return;
        }

        var starts =
            GetTextElementStarts();

        var bestIndex = 0;
        var bestDistance =
            float.MaxValue;

        foreach (var start in starts)
        {
            var width =
                _layoutContext
                    .MeasureText(
                        Text[..start],
                        FontSize)
                    .X;

            var distance =
                MathF.Abs(
                    width -
                    localX);

            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                bestIndex =
                    start;
            }
        }

        var endWidth =
            _layoutContext
                .MeasureText(
                    Text,
                    FontSize)
                .X;

        var endDistance =
            MathF.Abs(
                endWidth -
                localX);

        if (endDistance < bestDistance)
        {
            bestIndex =
                Text.Length;
        }

        CursorIndex =
            bestIndex;
    }

    private static int FindPreviousTextElementStart(
        int[] starts,
        int cursorIndex)
    {
        for (var i =
                 starts.Length - 1;
             i >= 0;
             i--)
        {
            if (starts[i] < cursorIndex)
            {
                return starts[i];
            }
        }

        return -1;
    }
}