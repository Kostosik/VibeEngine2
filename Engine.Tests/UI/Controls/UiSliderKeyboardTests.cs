using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Controls;

public sealed class UiSliderKeyboardTests
{
    [Fact]
    public void RightKey_IncreasesFocusedSliderValue()
    {
        var root =
            new UiRoot();

        var slider =
            new UiSlider
            {
                Value = 0.5f
            };

        root.AddChild(
            slider);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                300.0f,
                100.0f));

        var focus =
            new UiFocusManager();

        var textInput =
            new TestTextInput(
                TextInputKey.Right);

        _ =
            new UiInputRouter(
                root,
                new EmptyPointerInput(),
                textInput,
                focus);

        focus.SetFocus(
            slider);

        new UiInputRouter(
            root,
            new EmptyPointerInput(),
            textInput,
            focus).Update();

        Assert.Equal(
            0.55f,
            slider.Value,
            precision: 4);
    }

    [Fact]
    public void LeftKey_DecreasesFocusedSliderValue()
    {
        var root =
            new UiRoot();

        var slider =
            new UiSlider
            {
                Value = 0.5f
            };

        root.AddChild(
            slider);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                300.0f,
                100.0f));

        var focus =
            new UiFocusManager();

        var textInput =
            new TestTextInput(
                TextInputKey.Left);

        var router =
            new UiInputRouter(
                root,
                new EmptyPointerInput(),
                textInput,
                focus);

        focus.SetFocus(
            slider);

        router.Update();

        Assert.Equal(
            0.45f,
            slider.Value,
            precision: 4);
    }

    private sealed class TestTextInput : ITextInput
    {
        private readonly TextInputKey _pressedKey;

        public TestTextInput(
            TextInputKey pressedKey)
        {
            _pressedKey =
                pressedKey;
        }

        public IReadOnlyList<char> Characters =>
            Array.Empty<char>();

        public bool IsPressed(
            TextInputKey key)
        {
            return key ==
                   _pressedKey;
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