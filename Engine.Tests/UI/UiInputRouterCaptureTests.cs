using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI;

public sealed class UiInputRouterCaptureTests
{
    [Fact]
    public void CapturedWidget_WhenParentBecomesInvisible_StopsReceivingPointerEvents()
    {
        var root = new UiRoot();
        var parent = new TestContainer();
        var widget = new CapturingWidget();

        root.AddChild(parent);
        parent.AddChild(widget);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                100.0f,
                100.0f));

        var pointer = new TestPointerInput
        {
            Position = new Vector2(50.0f, 50.0f),
            LeftPressed = true
        };

        var textInput = new EmptyTextInput();
        var focus = new UiFocusManager();

        var router = new UiInputRouter(
            root,
            pointer,
            textInput,
            focus);

        router.Update();

        Assert.Equal(1, widget.PointerDownCount);
        Assert.Equal(1, widget.PointerMoveCount);

        pointer.LeftPressed = false;
        pointer.LeftDown = true;
        pointer.Position = new Vector2(60.0f, 60.0f);

        parent.Visible = false;

        router.Update();

        Assert.Equal(1, widget.PointerMoveCount);
        Assert.False(widget.IsPressed);

        pointer.LeftDown = false;
        pointer.LeftReleased = true;

        router.Update();

        Assert.Equal(0, widget.PointerUpCount);
    }

    private sealed class TestContainer : UiContainer
    {
    }

    private sealed class CapturingWidget : UiWidget
    {
        public int PointerDownCount { get; private set; }
        public int PointerMoveCount { get; private set; }
        public int PointerUpCount { get; private set; }

        protected override void OnPointerDown(
            UiPointerEvent pointer)
        {
            PointerDownCount++;
            IsPressed = true;
            pointer.RequestCapture();
        }

        protected override void OnPointerMove(
            UiPointerEvent pointer)
        {
            PointerMoveCount++;
        }

        protected override void OnPointerUp(
            UiPointerEvent pointer)
        {
            PointerUpCount++;
            IsPressed = false;
        }
    }

    private sealed class EmptyTextInput : ITextInput
    {
        public IReadOnlyList<char> Characters =>
            Array.Empty<char>();

        public bool IsPressed(TextInputKey key) =>
            false;
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
            return button == InputMouseButton.Left &&
                   LeftDown;
        }

        public bool IsPressed(
            InputMouseButton button)
        {
            return button == InputMouseButton.Left &&
                   LeftPressed;
        }

        public bool IsReleased(
            InputMouseButton button)
        {
            var released =
                button == InputMouseButton.Left &&
                LeftReleased;

            if (released)
            {
                LeftReleased = false;
            }

            return released;
        }
    }
}