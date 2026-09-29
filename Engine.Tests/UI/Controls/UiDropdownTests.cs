using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Controls;

public sealed class UiDropdownTests
{
    [Fact]
    public void ClearOptions_WhenPopupIsOpen_ClosesPopup()
    {
        var root =
            new UiRoot();

        var dropdown =
            new UiDropdown();

        dropdown.AddOption("First");

        root.AddChild(
            dropdown);

        dropdown.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                220.0f,
                42.0f));

        var pointer =
            new TestPointerInput
            {
                Position =
                    new Vector2(
                        20.0f,
                        20.0f)
            };

        var textInput =
            new EmptyTextInput();

        var focus =
            new UiFocusManager();

        var router =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        pointer.LeftPressed = true;

        router.Update();

        pointer.LeftPressed = false;
        pointer.LeftReleased = true;

        router.Update();

        Assert.True(
            dropdown.IsOpen);

        dropdown.ClearOptions();

        Assert.False(
            dropdown.IsOpen);

        Assert.Equal(
            -1,
            dropdown.SelectedIndex);

        Assert.Null(
            dropdown.SelectedText);
    }

    private sealed class TestPointerInput : IPointerInput
    {
        public Vector2 Position { get; set; }

        public float ScrollDelta { get; set; }

        public bool LeftDown { get; set; }

        public bool LeftPressed { get; set; }

        public bool LeftReleased { get; set; }

        public bool IsDown(
            InputMouseButton button)
        {
            return button ==
                   InputMouseButton.Left &&
                   LeftDown;
        }

        public bool IsPressed(
            InputMouseButton button)
        {
            return button ==
                   InputMouseButton.Left &&
                   LeftPressed;
        }

        public bool IsReleased(
            InputMouseButton button)
        {
            var released =
                button ==
                InputMouseButton.Left &&
                LeftReleased;

            if (released)
            {
                LeftReleased = false;
            }

            return released;
        }
    }

    private sealed class EmptyTextInput : ITextInput
    {
        public IReadOnlyList<char> Characters =>
            Array.Empty<char>();

        public bool IsPressed(
            TextInputKey key)
        {
            return false;
        }
    }
}