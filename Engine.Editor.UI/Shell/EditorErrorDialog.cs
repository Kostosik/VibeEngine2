using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

public sealed class EditorErrorDialog :
    UiModal
{
    private readonly UiLabel _message;

    public EditorErrorDialog(
        UiOverlayLayer overlay,
        UiFocusManager focus)
        : base(
            overlay,
            focus)
    {
        SetSize(
            560.0f,
            220.0f);

        CloseOnEscape = true;

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

        content.AddChild(
            new UiLabel(
                "Operation failed")
            {
                FontSize = 18.0f,
                Height = 28.0f
            });

        _message =
            new UiLabel()
            {
                FontSize = 14.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        content.AddChild(
            _message);

        var okButton =
            new UiButton("OK")
            {
                Width = 90.0f,
                Height = 32.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Right
            };

        okButton.Clicked +=
            Close;

        content.AddChild(
            okButton);

        SetContent(
            content);
    }

    public void ShowError(
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        _message.Text =
            message;

        Show();
    }
}