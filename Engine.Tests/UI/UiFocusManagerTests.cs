using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.Tests.UI;

public sealed class UiFocusManagerTests
{
    [Fact]
    public void FocusedWidget_WhenParentBecomesInvisible_LosesFocus()
    {
        var root =
            new UiRoot();

        var parent =
            new UiPanel();

        var button =
            new UiButton("Test");

        root.AddChild(
            parent);

        parent.AddChild(
            button);

        var focus =
            new UiFocusManager();

        _ =
            new UiInputRouter(
                root,
                new TestPointerInput(),
                new TestTextInput(),
                focus);

        focus.SetFocus(
            button);

        Assert.Same(
            button,
            focus.FocusedWidget);

        parent.Visible =
            false;

        focus.ValidateFocus();

        Assert.Null(
            focus.FocusedWidget);

        Assert.False(
            button.IsFocused);
    }

    [Fact]
    public void FocusedWidget_WhenParentBecomesDisabled_LosesFocus()
    {
        var root =
            new UiRoot();

        var parent =
            new UiPanel();

        var button =
            new UiButton("Test");

        root.AddChild(
            parent);

        parent.AddChild(
            button);

        var focus =
            new UiFocusManager();

        _ =
            new UiInputRouter(
                root,
                new TestPointerInput(),
                new TestTextInput(),
                focus);

        focus.SetFocus(
            button);

        parent.Enabled =
            false;

        focus.ValidateFocus();

        Assert.Null(
            focus.FocusedWidget);

        Assert.False(
            button.IsFocused);
    }

    private sealed class TestPointerInput :
        IPointerInput
    {
        public Engine.Core.Math.Vector2 Position =>
            Engine.Core.Math.Vector2.Zero;

        public float ScrollDelta =>
            0.0f;

        public bool IsDown(
            InputMouseButton button)
        {
            return false;
        }

        public bool IsPressed(
            InputMouseButton button)
        {
            return false;
        }

        public bool IsReleased(
            InputMouseButton button)
        {
            return false;
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