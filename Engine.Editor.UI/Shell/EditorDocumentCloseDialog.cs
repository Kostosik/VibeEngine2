using Engine.Editor.Documents;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using System.Runtime.InteropServices;

namespace Engine.Editor.UI.Shell;

public sealed class EditorDocumentCloseDialog :
    UiModal
{
    private readonly UiLabel _message;
    private readonly UiButton _saveButton;

    public EditorDocumentCloseDialog(
        UiOverlayLayer overlay,
        UiFocusManager focus)
        : base(
            overlay,
            focus)
    {
        SetSize(
            420.0f,
            180.0f);

        CloseOnEscape =
            true;

        var content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 14.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        _message =
            new UiLabel(
                "Save changes?")
            {
                Height = 50.0f,
                FontSize = 16.0f,
                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        content.AddChild(
            _message);

        var buttons =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 8.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Right,

                VerticalAlignment =
                    UiVerticalAlignment.Bottom
            };

        _saveButton =
            new UiButton("Save")
            {
                Width = 90.0f,
                Height = 32.0f
            };

        _saveButton.Clicked +=
            () =>
            {
                SaveRequested?.Invoke();
            };

        var discardButton =
            new UiButton("Discard")
            {
                Width = 90.0f,
                Height = 32.0f
            };

        discardButton.Clicked +=
            () =>
            {
                DiscardRequested?.Invoke();
            };

        var cancelButton =
            new UiButton("Cancel")
            {
                Width = 90.0f,
                Height = 32.0f
            };

        cancelButton.Clicked +=
            Close;

        buttons.AddChild(
            _saveButton);

        buttons.AddChild(
            discardButton);

        buttons.AddChild(
            cancelButton);

        content.AddChild(
            buttons);

        SetContent(
            content);
    }

    public event Action? SaveRequested;
    public event Action? DiscardRequested;

    public void ShowFor(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        var name =
            document.FilePath is null
                ? "Untitled document"
                : Path.GetFileName(
                    document.FilePath);

        _message.Text =
            $"Save changes to \"{name}\"?";

        _saveButton.Enabled =
            document.FilePath is not null;

        Show(
            _saveButton);
    }
}