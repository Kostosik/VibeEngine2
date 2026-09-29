using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Controls;

public sealed class UiInteractiveControlRoutingTests
{
    [Fact]
    public void Toggle_Click_DoesNotBubbleToParent()
    {
        var root =
            new UiRoot();

        var parent =
            new TestContainer();

        var toggle =
            new UiToggle("Test");

        root.AddChild(parent);
        parent.AddChild(toggle);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                300.0f,
                100.0f));

        var pointer =
            new TestPointerInput
            {
                Position =
                    new Vector2(
                        20.0f,
                        20.0f),

                LeftPressed = true
            };

        var router =
            new UiInputRouter(
                root,
                pointer,
                new EmptyTextInput(),
                new UiFocusManager());

        router.Update();

        Assert.Equal(
            0,
            parent.PointerDownCount);
    }

    [Fact]
    public void TextBox_Click_DoesNotBubbleToParent()
    {
        var root =
            new UiRoot();

        var parent =
            new TestContainer();

        var textBox =
            new UiTextBox();

        root.AddChild(parent);
        parent.AddChild(textBox);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                300.0f,
                100.0f));

        var pointer =
            new TestPointerInput
            {
                Position =
                    new Vector2(
                        20.0f,
                        20.0f),

                LeftPressed = true
            };

        var router =
            new UiInputRouter(
                root,
                pointer,
                new EmptyTextInput(),
                new UiFocusManager());

        router.Update();

        Assert.Equal(
            0,
            parent.PointerDownCount);
    }

    private sealed class TestContainer : UiContainer
    {
        public int PointerDownCount { get; private set; }

        protected override void OnPointerDown(
            UiPointerEvent pointer)
        {
            PointerDownCount++;
        }
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