using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.Graphics.Commands;


namespace Engine.Editor.UI.Dialogs;

public sealed class EditorUiActionPickerDialog
{
    private readonly UiModal _modal;
    private readonly UiStackPanel _actionsPanel;
    private readonly UiLabel _emptyLabel;
    private readonly UiTextBox _actionIdInput;

    private Action<string>? _selected;

    public EditorUiActionPickerDialog(
        UiOverlayLayer overlays,
        UiFocusManager focus)
    {
        ArgumentNullException.ThrowIfNull(overlays);
        ArgumentNullException.ThrowIfNull(focus);

        _modal = new UiModal(overlays, focus);
        _modal.SetSize(620.0f, 560.0f);

        var title = new UiLabel("Select UI Action")
        {
            Height = 28.0f,
            FontSize = 18.0f
        };

        var description = new UiLabel(
            "Choose an existing Action ID or enter one below.")
        {
            Height = 24.0f,
            FontSize = 12.0f
        };

        _actionsPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var scroll = new UiScrollView
        {
            Height = 270.0f,
            VerticalScrolling = true,
            HorizontalScrolling = false,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        scroll.SetContent(_actionsPanel);

        _emptyLabel = new UiLabel(
            "No existing actions. Enter an Action ID below.")
        {
            Height = 24.0f,
            FontSize = 12.0f,
            Color = new UiColor(150, 150, 150, 255)
        };

        var inputLabel = new UiLabel("Enter or change Action ID")
        {
            Height = 22.0f,
            FontSize = 12.0f
        };

        _actionIdInput = new UiTextBox
        {
            Width = 390.0f,
            Height = 34.0f,
            FontSize = 12.0f,
            Placeholder = "Enter Action ID",
            MaxLength = 256
        };

        _actionIdInput.Submitted += _ => UseEnteredAction();

        var useButton = new UiButton("Use ID")
        {
            Width = 84.0f,
            Height = 34.0f,
            FontSize = 12.0f
        };

        useButton.Clicked += UseEnteredAction;

        var inputRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 34.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        inputRow.AddChild(_actionIdInput);
        inputRow.AddChild(useButton);

        var cancelButton = new UiButton("Cancel")
        {
            Width = 84.0f,
            Height = 34.0f
        };

        cancelButton.Clicked += _modal.Close;

        var footer = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Height = 34.0f,
            HorizontalAlignment = UiHorizontalAlignment.Right
        };

        footer.AddChild(cancelButton);

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            Width = 580.0f,
            Height = 520.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        content.AddChild(title);
        content.AddChild(description);
        content.AddChild(
            new UiLabel("Existing Action IDs")
            {
                Height = 22.0f,
                FontSize = 12.0f
            });
        content.AddChild(scroll);
        content.AddChild(_emptyLabel);
        content.AddChild(inputLabel);
        content.AddChild(inputRow);
        content.AddChild(footer);

        _modal.SetContent(content);
        _modal.Closed += ClearRequest;
    }

    public bool IsOpen => _modal.IsOpen;

    public bool Show(
        IReadOnlyList<string> actionIds,
        string? currentAction,
        Action<string> selected)
    {
        ArgumentNullException.ThrowIfNull(actionIds);
        ArgumentNullException.ThrowIfNull(selected);

        if (_modal.IsOpen)
        {
            _modal.Close();
        }

        _selected = selected;
        _actionIdInput.SetText(currentAction ?? string.Empty);
        _actionsPanel.ClearChildren();

        var actions = actionIds
            .Append(currentAction ?? string.Empty)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static id => id, StringComparer.Ordinal)
            .ToArray();

        foreach (var action in actions)
        {
            var button = new UiButton(action)
            {
                Width = 520.0f,
                Height = 32.0f,
                FontSize = 12.0f,
                HorizontalTextAlignment = UiTextHorizontalAlignment.Left
            };

            button.Clicked += () => SelectAction(action);
            _actionsPanel.AddChild(button);
        }

        _emptyLabel.Visible = actions.Length == 0;

        _modal.Show(_actionIdInput);
        return true;
    }

    public void Close()
    {
        _modal.Close();
    }

    private void UseEnteredAction()
    {
        var action = _actionIdInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(action))
        {
            return;
        }

        SelectAction(action);
    }

    private void SelectAction(string action)
    {
        var callback = _selected;

        _modal.Close();

        callback?.Invoke(action);
    }

    private void ClearRequest()
    {
        _selected = null;
    }
}