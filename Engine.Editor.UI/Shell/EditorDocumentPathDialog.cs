using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

public sealed class EditorDocumentPathDialog :
    UiModal
{
    private readonly UiTextBox _pathBox;

    public EditorDocumentPathDialog(
        UiOverlayLayer overlay,
        UiFocusManager focus)
        : base(
            overlay,
            focus)
    {
        SetSize(
            560.0f,
            180.0f);

        CloseOnEscape =
            true;

        var content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 12.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        _pathBox =
            new UiTextBox
            {
                Placeholder =
                    "Path to .world file",

                Width = 500.0f,
                Height = 42.0f
            };

        _pathBox.Submitted +=
            Submit;

        content.AddChild(
            _pathBox);

        var buttons =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Horizontal,

                Spacing = 8.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Right
            };

        var submitButton =
            new UiButton("Select")
            {
                Width = 90.0f,
                Height = 32.0f
            };

        submitButton.Clicked +=
            () =>
            {
                Submit(
                    _pathBox.Text);
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
            submitButton);

        buttons.AddChild(
            cancelButton);

        content.AddChild(
            buttons);

        SetContent(
            content);
    }

    public event Action<string>? PathSubmitted;

    public void ShowFor()
    {
        Show(
            _pathBox);
    }

    private void Submit(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        PathSubmitted?.Invoke(
            path.Trim());

        Close();
    }
}