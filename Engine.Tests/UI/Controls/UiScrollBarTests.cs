using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Controls;

public sealed class UiScrollBarTests
{
    [Fact]
    public void Value_IsClampedToNormalizedRange()
    {
        var scrollBar =
            new UiScrollBar(
                UiScrollBarOrientation.Vertical);

        scrollBar.Value = -1.0f;

        Assert.Equal(
            0.0f,
            scrollBar.Value);

        scrollBar.Value = 2.0f;

        Assert.Equal(
            1.0f,
            scrollBar.Value);
    }

    [Fact]
    public void DraggingThumb_ChangesValue()
    {
        var root =
            new UiRoot();

        var scrollBar =
            new UiScrollBar(
                UiScrollBarOrientation.Vertical)
            {
                Width = 14.0f,
                Height = 100.0f,
                ViewportRatio = 0.25f
            };

        root.AddChild(
            scrollBar);

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
                        7.0f,
                        10.0f)
            };

        var router =
            new UiInputRouter(
                root,
                pointer,
                new EmptyTextInput(),
                new UiFocusManager());

        pointer.LeftPressed = true;

        router.Update();

        pointer.LeftPressed = false;
        pointer.LeftDown = true;
        pointer.Position =
            new Vector2(
                7.0f,
                70.0f);

        router.Update();

        Assert.True(
            scrollBar.Value > 0.0f);

        pointer.LeftDown = false;
        pointer.LeftReleased = true;

        router.Update();
    }

    [Fact]
    public void DraggingThumb_CanContinueOutsideOriginalBounds()
    {
        var root =
            new UiRoot();

        var scrollBar =
            new UiScrollBar(
                UiScrollBarOrientation.Vertical)
            {
                Width = 14.0f,
                Height = 100.0f,
                ViewportRatio = 0.25f
            };

        root.AddChild(
            scrollBar);

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
                        7.0f,
                        10.0f)
            };

        var router =
            new UiInputRouter(
                root,
                pointer,
                new EmptyTextInput(),
                new UiFocusManager());

        pointer.LeftPressed = true;

        router.Update();

        pointer.LeftPressed = false;
        pointer.LeftDown = true;
        pointer.Position =
            new Vector2(
                7.0f,
                140.0f);

        router.Update();

        Assert.Equal(
            1.0f,
            scrollBar.Value);

        pointer.LeftDown = false;
        pointer.LeftReleased = true;

        router.Update();
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