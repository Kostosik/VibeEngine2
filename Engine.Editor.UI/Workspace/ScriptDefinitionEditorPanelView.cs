using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Scripting;
using Engine.Tooling.Debugging;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class ScriptDefinitionEditorPanelView : UiPanel
{
    private readonly ScriptDefinitionDocument _document;
    private readonly EditorContext _editor;
    private readonly DebugConsole? _console;
    private readonly UiTextBox _scriptIdTextBox = new();
    private readonly UiDropdown _classDropdown;
    private readonly UiButton _refreshButton;
    private readonly UiButton _assignButton;
    private readonly UiLabel _implementationLabel;
    private readonly UiLabel _statusLabel;
    public ScriptDefinitionDocument Document => _document;
    private readonly List<ScriptImplementationChoice> _choices = new();

    private ScriptImplementationChoice? _selectedChoice;

    public ScriptDefinitionEditorPanelView(
        ScriptDefinitionDocument document,
        EditorContext editor,
        DebugConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(editor);

        _document = document;
        _editor = editor;
        _console = console;

        Padding = new UiThickness(12.0f);
        Background = new UiColor(28, 28, 28, 255);

        var layout = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 10.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        layout.AddChild(
            new UiLabel("Script Editor")
            {
                Height = 32.0f,
                FontSize = 22.0f
            });

        layout.AddChild(
            new UiLabel(document.DisplayName)
            {
                Height = 24.0f,
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170, 255)
            });

        _scriptIdTextBox.SetText(
            _document.Definition.ScriptId);

        _scriptIdTextBox.Width = 280.0f;
        _scriptIdTextBox.Height = 30.0f;
        _scriptIdTextBox.FontSize = 13.0f;
        _scriptIdTextBox.MaxLength = 256;
        _scriptIdTextBox.Placeholder = "game.my_script";

        var scriptIdRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 32.0f
        };

        scriptIdRow.AddChild(
            new UiLabel("Script ID")
            {
                Width = 120.0f,
                Height = 30.0f,
                FontSize = 13.0f
            });

        scriptIdRow.AddChild(_scriptIdTextBox);

        var applyScriptIdButton = new UiButton("Apply ID")
        {
            Width = 100.0f,
            Height = 30.0f
        };

        applyScriptIdButton.Clicked += ApplyScriptId;

        scriptIdRow.AddChild(applyScriptIdButton);
        layout.AddChild(scriptIdRow);

        layout.AddChild(
            new UiLabel("C# implementation")
            {
                FontSize = 14.0f
            });

        _implementationLabel = new UiLabel(string.Empty)
        {
            FontSize = 12.0f,
            Color = new UiColor(180, 200, 180, 255)
        };

        layout.AddChild(_implementationLabel);

        _classDropdown = new UiDropdown
        {
            Width = 560.0f,
            Height = 42.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        _classDropdown.SelectionChanged +=
            (index, _) =>
            {
                _selectedChoice =
                    index >= 0 && index < _choices.Count
                        ? _choices[index]
                        : null;

                _assignButton.Enabled =
                    _selectedChoice is not null;
            };

        layout.AddChild(_classDropdown);

        _statusLabel = new UiLabel(
            "Select the active game project, then find available C# classes.")
        {
            FontSize = 12.0f,
            Color = new UiColor(200, 180, 130, 255)
        };

        layout.AddChild(_statusLabel);

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 36.0f
        };

        _refreshButton = new UiButton("Find C# Classes")
        {
            Width = 150.0f,
            Height = 34.0f
        };

        _refreshButton.Clicked += RefreshClassList;

        _assignButton = new UiButton("Assign Class")
        {
            Width = 130.0f,
            Height = 34.0f,
            Enabled = false
        };

        _assignButton.Clicked += AssignSelectedClass;

        buttons.AddChild(_refreshButton);
        buttons.AddChild(_assignButton);

        layout.AddChild(buttons);

        layout.AddChild(
            new UiLabel(
                "Classes must implement IEntityScript and have a public " +
                "parameterless constructor. Save the resource after changing its class.")
            {
                FontSize = 11.0f,
                Color = new UiColor(155, 155, 155, 255)
            });

        AddChild(layout);

        RefreshImplementationLabel();
    }

    private void ApplyScriptId()
    {
        var scriptId = _scriptIdTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(scriptId))
        {
            _scriptIdTextBox.SetText(
                _document.Definition.ScriptId);

            return;
        }

        _document.UpdateScriptId(scriptId);
    }

    private async void RefreshClassList()
    {
        var project = _editor.ActiveGameProject;

        if (project is null)
        {
            _statusLabel.Text =
                "Open a game project before searching for C# classes.";

            return;
        }

        _refreshButton.Enabled = false;
        _assignButton.Enabled = false;
        _selectedChoice = null;

        _choices.Clear();
        _classDropdown.ClearOptions();

        _statusLabel.Text =
            "Building the game project and searching for script classes...";

        try
        {
            var choices =
                await ScriptImplementationDiscovery.DiscoverAsync(
                    project,
                    _console);

            _choices.AddRange(choices);

            foreach (var choice in _choices)
            {
                _classDropdown.AddOption(
                    choice.DisplayName);
            }

            var current = _document.Definition;

            var selectedIndex = _choices.FindIndex(
                choice =>
                    string.Equals(
                        choice.AssemblyName,
                        current.ImplementationAssemblyName,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        choice.TypeName,
                        current.ImplementationTypeName,
                        StringComparison.Ordinal));

            if (selectedIndex >= 0)
            {
                _classDropdown.SetSelectedIndex(selectedIndex);
            }

            _statusLabel.Text =
                _choices.Count == 0
                    ? "No compatible C# script classes were found."
                    : $"Found {_choices.Count} compatible class(es). " +
                      "Select a class and click Assign Class.";

            _assignButton.Enabled =
                _selectedChoice is not null;
        }
        catch (Exception exception)
        {
            _statusLabel.Text =
                $"Could not find C# classes: {exception.Message}";

            _console?.WriteException(
                exception,
                "Failed to discover C# script implementations.",
                "SCRIPT_DISCOVERY_FAILED");
        }
        finally
        {
            _refreshButton.Enabled = true;
        }
    }

    private void AssignSelectedClass()
    {
        if (_selectedChoice is not { } choice)
        {
            return;
        }

        try
        {
            _document.UpdateImplementation(
                choice.AssemblyName,
                choice.TypeName);

            RefreshImplementationLabel();

            _statusLabel.Text =
                $"Assigned '{choice.TypeName}'. Save the resource to persist the change.";

            _console?.WriteLine(
                $"Assigned C# class '{choice.TypeName}' " +
                $"from '{choice.AssemblyName}' to " +
                $"'{_document.Definition.ScriptId}'.");
        }
        catch (Exception exception)
        {
            _statusLabel.Text = exception.Message;

            _console?.WriteException(
                exception,
                "Failed to assign the C# script class.");
        }
    }

    private void RefreshImplementationLabel()
    {
        var definition = _document.Definition;

        _implementationLabel.Text =
            definition.HasImplementation
                ? $"{definition.ImplementationTypeName} " +
                  $"({definition.ImplementationAssemblyName})"
                : "No C# implementation assigned.";
    }
}