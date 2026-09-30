using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Controls;

public sealed class UiTextBoxTests
{
    [Fact]
    public void CursorNavigation_AllowsInsertionInMiddleOfText()
    {
        var root =
            new UiRoot();

        var textBox =
            new UiTextBox("abc");

        root.AddChild(
            textBox);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                300.0f,
                60.0f));

        var pointer =
            new EmptyPointerInput();

        var textInput =
            new TestTextInput();

        var focus =
            new UiFocusManager();

        var router =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        focus.SetFocus(
            textBox);

        Assert.Equal(
            3,
            textBox.CursorIndex);

        textInput.Press(
            TextInputKey.Left);

        router.Update();

        Assert.Equal(
            2,
            textBox.CursorIndex);

        textInput.Release();

        textInput.SetCharacters(
            'X');

        router.Update();

        Assert.Equal(
            "abXc",
            textBox.Text);

        Assert.Equal(
            3,
            textBox.CursorIndex);
    }

    [Fact]
    public void Backspace_RemovesTextElementBeforeCursor()
    {
        var root =
            new UiRoot();

        var textBox =
            new UiTextBox("abc");

        root.AddChild(
            textBox);

        var pointer =
            new EmptyPointerInput();

        var textInput =
            new TestTextInput();

        var focus =
            new UiFocusManager();

        var router =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        focus.SetFocus(
            textBox);

        textInput.Press(
            TextInputKey.Left);

        router.Update();

        textInput.Release();

        textInput.Press(
            TextInputKey.Backspace);

        router.Update();

        Assert.Equal(
            "ac",
            textBox.Text);

        Assert.Equal(
            1,
            textBox.CursorIndex);
    }

    [Fact]
    public void Backspace_DoesNotSplitCombiningCharacter()
    {
        const string text =
            "a\u0301b";

        var root =
            new UiRoot();

        var textBox =
            new UiTextBox(text);

        root.AddChild(
            textBox);

        var pointer =
            new EmptyPointerInput();

        var textInput =
            new TestTextInput();

        var focus =
            new UiFocusManager();

        var router =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        focus.SetFocus(
            textBox);

        textInput.Press(
            TextInputKey.Left);

        router.Update();

        textInput.Release();

        textInput.Press(
            TextInputKey.Backspace);

        router.Update();

        Assert.Equal(
            "b",
            textBox.Text);
    }

    private sealed class TestTextInput :
        ITextInput
    {
        private TextInputKey? _pressed;
        private IReadOnlyList<char> _characters =
            Array.Empty<char>();

        public IReadOnlyList<char> Characters =>
            _characters;

        public bool IsPressed(
            TextInputKey key)
        {
            return _pressed == key;
        }

        public void Press(
            TextInputKey key)
        {
            _pressed = key;
            _characters =
                Array.Empty<char>();
        }

        public void Release()
        {
            _pressed = null;
            _characters =
                Array.Empty<char>();
        }

        public void SetCharacters(
            params char[] characters)
        {
            _pressed = null;
            _characters = characters;
        }
    }

    private sealed class EmptyPointerInput :
        IPointerInput
    {
        public Vector2 Position =>
            Vector2.Zero;

        public float ScrollDelta =>
            0.0f;

        public bool IsDown(
            InputMouseButton button) =>
            false;

        public bool IsPressed(
            InputMouseButton button) =>
            false;

        public bool IsReleased(
            InputMouseButton button) =>
            false;
    }
}