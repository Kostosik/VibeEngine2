
using Engine.Editor.Assets;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.Graphics.Commands;

namespace Engine.Editor.UI.Shell;

public sealed class EditorAssetCreationDialog : UiModal
{
    private readonly EditorAssetCreationRegistry _registry;
    private readonly UiDropdown _typeDropdown;
    private readonly UiTextBox _nameBox;
    private readonly UiLabel _detailsLabel;
    private readonly UiLabel _messageLabel;

    private EditorAssetCreationDefinition? _selectedDefinition;
    private string? _targetDirectory;

    public EditorAssetCreationDialog(
        UiOverlayLayer overlay,
        UiFocusManager focus,
        EditorAssetCreationRegistry registry)
        : base(overlay, focus)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (registry.Definitions.Count == 0)
        {
            throw new ArgumentException(
                "At least one asset creation type must be registered.",
                nameof(registry));
        }

        _registry = registry;

        SetSize(520.0f, 320.0f);
        CloseOnEscape = true;

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        content.AddChild(new UiLabel("Create")
        {
            Height = 28.0f,
            FontSize = 19.0f
        });

        content.AddChild(new UiLabel("Type")
        {
            Height = 20.0f,
            FontSize = 13.0f
        });

        _typeDropdown = new UiDropdown
        {
            Width = 460.0f,
            Height = 32.0f
        };

        foreach (var definition in _registry.Definitions)
        {
            _typeDropdown.AddOption(definition.DisplayName);
        }

        content.AddChild(_typeDropdown);

        content.AddChild(new UiLabel("Name")
        {
            Height = 20.0f,
            FontSize = 13.0f
        });

        _nameBox = new UiTextBox
        {
            Width = 460.0f,
            Height = 34.0f,
            FontSize = 14.0f,
            MaxLength = 128,
            Placeholder = "Enter a name"
        };

        _nameBox.Submitted += _ => Submit();
        _nameBox.TextChanged += _ => _messageLabel!.Text = string.Empty;

        content.AddChild(_nameBox);

        _detailsLabel = new UiLabel
        {
            Height = 20.0f,
            FontSize = 12.0f,
            Color = new UiColor(160, 160, 160, 255)
        };

        content.AddChild(_detailsLabel);

        _messageLabel = new UiLabel
        {
            Height = 20.0f,
            FontSize = 12.0f,
            Color = new UiColor(240, 150, 120, 255)
        };

        content.AddChild(_messageLabel);

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Right
        };

        var createButton = new UiButton("Create")
        {
            Width = 100.0f,
            Height = 32.0f
        };

        createButton.Clicked += Submit;

        var cancelButton = new UiButton("Cancel")
        {
            Width = 100.0f,
            Height = 32.0f
        };

        cancelButton.Clicked += Close;

        buttons.AddChild(createButton);
        buttons.AddChild(cancelButton);
        content.AddChild(buttons);

        _typeDropdown.SelectionChanged += OnTypeChanged;

        SetContent(content);

        _typeDropdown.SetSelectedIndex(0);
    }

    public event Action<
        EditorAssetCreationDefinition,
        string,
        string?>? CreateRequested;

    public void ShowFor(string? targetDirectory = null)
    {
        _targetDirectory = targetDirectory;
        _messageLabel.Text = string.Empty;

        _typeDropdown.SetSelectedIndex(0);

        if (_selectedDefinition is not null)
        {
            _nameBox.SetText(_selectedDefinition.DefaultName);
            UpdateDetails(_selectedDefinition);
        }

        Show(_nameBox);
    }

    private void OnTypeChanged(int index, string _)
    {
        if (index < 0 || index >= _registry.Definitions.Count)
        {
            return;
        }

        _selectedDefinition = _registry.Definitions[index];

        _nameBox.SetText(_selectedDefinition.DefaultName);
        _messageLabel.Text = string.Empty;

        UpdateDetails(_selectedDefinition);
    }

    private void UpdateDetails(
        EditorAssetCreationDefinition definition)
    {
        _detailsLabel.Text =
            definition.Kind == EditorAssetCreationKind.Directory
                ? "Creates a folder at the selected location."
                : $"Extension: {definition.Extension}";
    }

    private void Submit()
    {
        var definition = _selectedDefinition;

        if (definition is null)
        {
            return;
        }

        var name = _nameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            _messageLabel.Text = "Enter a name.";
            return;
        }

        var targetDirectory = _targetDirectory;

        Close();

        CreateRequested?.Invoke(
            definition,
            name,
            targetDirectory);
    }
}
