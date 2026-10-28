using Engine.Editor.UI.Dialogs;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.Tests.Editor;

public sealed class EditorUiActionPickerDialogTests
{
    [Fact]
    public void Show_SelectingExistingAction_InvokesCallbackAndCloses()
    {
        var overlays = new UiOverlayLayer();
        var focus = new UiFocusManager();
        var dialog = new EditorUiActionPickerDialog(overlays, focus);

        string? selectedAction = null;

        Assert.True(
            dialog.Show(
                new[] { "StartGameRequested" },
                null,
                action => selectedAction = action));

        var actionButton = Find<UiButton>(
            overlays,
            button => button.Text == "StartGameRequested");

        Assert.NotNull(actionButton);

        actionButton!.Click();

        Assert.Equal("StartGameRequested", selectedAction);
        Assert.False(dialog.IsOpen);
    }

    [Fact]
    public void Show_UseIdAcceptsNewActionId()
    {
        var overlays = new UiOverlayLayer();
        var focus = new UiFocusManager();
        var dialog = new EditorUiActionPickerDialog(overlays, focus);

        string? selectedAction = null;

        dialog.Show(
            Array.Empty<string>(),
            null,
            action => selectedAction = action);

        var input = Find<UiTextBox>(
            overlays,
            textBox => textBox.Placeholder == "Enter Action ID");

        Assert.NotNull(input);

        input!.SetText("  game.pause  ");

        var useButton = Find<UiButton>(
            overlays,
            button => button.Text == "Use ID");

        Assert.NotNull(useButton);

        useButton!.Click();

        Assert.Equal("game.pause", selectedAction);
        Assert.False(dialog.IsOpen);
    }

    private static T? Find<T>(
        UiWidget widget,
        Func<T, bool> predicate)
        where T : UiWidget
    {
        if (widget is T candidate && predicate(candidate))
        {
            return candidate;
        }

        if (widget is not UiContainer container)
        {
            return null;
        }

        foreach (var child in container.Children)
        {
            var result = Find(child, predicate);

            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}