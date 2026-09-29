using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI;

public sealed class UiInputRouterTests
{
    [Fact]
    public void RightPointerDownAndUp_AreRoutedToTarget()
    {
        var root =
            new UiRoot();

        var widget =
            new TestWidget();

        root.AddChild(
            widget);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                100.0f,
                100.0f));

        var pointer =
            new TestPointerInput
            {
                Position =
                    new Vector2(
                        50.0f,
                        50.0f),

                RightPressed = true
            };

        var textInput =
            new TestTextInput();

        var focus =
            new UiFocusManager();

        _ =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        pointer.RightPressed =
            true;

        pointer.RightReleased =
            false;

        var router =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        router.Update();

        Assert.Equal(
            1,
            widget.PointerDownCount);

        Assert.Equal(
            InputMouseButton.Right,
            widget.LastDownButton);

        pointer.RightPressed =
            false;

        pointer.RightReleased =
            true;

        router.Update();

        Assert.Equal(
            1,
            widget.PointerUpCount);

        Assert.Equal(
            InputMouseButton.Right,
            widget.LastUpButton);
    }

    private sealed class TestWidget :
        UiPanel
    {
        public int PointerDownCount { get; private set; }

        public int PointerUpCount { get; private set; }

        public InputMouseButton LastDownButton { get; private set; }

        public InputMouseButton LastUpButton { get; private set; }

        protected override void OnPointerDown(
            UiPointerEvent pointer)
        {
            PointerDownCount++;
            LastDownButton =
                pointer.Button;
        }

        protected override void OnPointerUp(
            UiPointerEvent pointer)
        {
            PointerUpCount++;
            LastUpButton =
                pointer.Button;
        }
    }

    private sealed class TestPointerInput :
        IPointerInput
    {
        public Vector2 Position { get; set; }

        public float ScrollDelta =>
            0.0f;

        public bool RightPressed { get; set; }

        public bool RightReleased { get; set; }

        public bool IsDown(
            InputMouseButton button)
        {
            return button ==
                   InputMouseButton.Right &&
                   RightPressed;
        }

        public bool IsPressed(
            InputMouseButton button)
        {
            return button ==
                   InputMouseButton.Right &&
                   RightPressed;
        }

        public bool IsReleased(
            InputMouseButton button)
        {
            return button ==
                   InputMouseButton.Right &&
                   RightReleased;
        }
    }

    private sealed class TestTextInput :
        ITextInput
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