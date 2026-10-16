
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

public sealed class EditorNewDocumentDialog : UiModal
{
    private readonly UiButton _worldButton;

    public EditorNewDocumentDialog(
        UiOverlayLayer overlay,
        UiFocusManager focus)
        : base(overlay, focus)
    {
        SetSize(420.0f, 160.0f);

        CloseOnEscape = true;

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 14.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        content.AddChild(new UiLabel("Choose document type")
        {
            Height = 44.0f,
            FontSize = 16.0f
        });

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Right
        };

        _worldButton = new UiButton("World")
        {
            Width = 100.0f,
            Height = 32.0f
        };

        _worldButton.Clicked += SelectWorld;

        var uiButton = new UiButton("UI")
        {
            Width = 100.0f,
            Height = 32.0f
        };

        uiButton.Clicked += SelectUi;

        var cancelButton = new UiButton("Cancel")
        {
            Width = 100.0f,
            Height = 32.0f
        };

        cancelButton.Clicked += Close;

        buttons.AddChild(_worldButton);
        buttons.AddChild(uiButton);
        buttons.AddChild(cancelButton);

        content.AddChild(buttons);

        SetContent(content);
    }

    public event Action? WorldRequested;
    public event Action? UiRequested;

    public void ShowFor()
    {
        Show(_worldButton);
    }

    private void SelectWorld()
    {
        Close();
        WorldRequested?.Invoke();
    }

    private void SelectUi()
    {
        Close();
        UiRequested?.Invoke();
    }
}
