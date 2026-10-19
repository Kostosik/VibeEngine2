using Engine.Editor.Projects;
using Engine.Editor.UI.Dialogs;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Shell;

/// <summary>
/// Collects the user-facing settings for creating a project.
/// Project generation is delegated to the application layer.
/// </summary>
public sealed class EditorNewProjectDialog : UiModal
{
    private readonly IEditorFileDialogService _fileDialogs;
    private readonly UiTextBox _nameBox;
    private readonly UiTextBox _locationBox;
    private readonly UiLabel _previewLabel;
    private readonly UiLabel _messageLabel;

    public EditorNewProjectDialog(
        UiOverlayLayer overlay,
        UiFocusManager focus,
        IEditorFileDialogService fileDialogs)
        : base(overlay, focus)
    {
        ArgumentNullException.ThrowIfNull(fileDialogs);

        _fileDialogs = fileDialogs;

        SetSize(650.0f, 390.0f);
        CloseOnEscape = true;

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        content.AddChild(new UiLabel("Project name")
        {
            Height = 22.0f,
            FontSize = 15.0f
        });

        _nameBox = new UiTextBox
        {
            Width = 570.0f,
            Height = 38.0f,
            MaxLength = 100,
            Placeholder = "MyGame"
        };
        _nameBox.TextChanged += OnInputChanged;
        _nameBox.Submitted += _ => RequestCreate();
        content.AddChild(_nameBox);

        content.AddChild(new UiLabel("Parent folder")
        {
            Height = 22.0f,
            FontSize = 15.0f
        });

        var locationRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _locationBox = new UiTextBox
        {
            Width = 460.0f,
            Height = 38.0f,
            MaxLength = 1024,
            Placeholder = "Choose a folder for your projects"
        };
        _locationBox.TextChanged += OnInputChanged;

        var browseButton = new UiButton("Browse…")
        {
            Width = 100.0f,
            Height = 38.0f
        };
        browseButton.Clicked += BrowseForFolder;

        locationRow.AddChild(_locationBox);
        locationRow.AddChild(browseButton);
        content.AddChild(locationRow);

        _previewLabel = new UiLabel
        {
            Height = 28.0f,
            FontSize = 13.0f,
            Color = new Engine.Core.Math.UiColor(175, 175, 175, 255)
        };
        content.AddChild(_previewLabel);

        _messageLabel = new UiLabel
        {
            Height = 34.0f,
            FontSize = 13.0f,
            Color = new Engine.Core.Math.UiColor(230, 130, 120, 255)
        };
        content.AddChild(_messageLabel);

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Right,
            VerticalAlignment = UiVerticalAlignment.Bottom
        };

        var createButton = new UiButton("Create Project")
        {
            Width = 130.0f,
            Height = 34.0f
        };
        createButton.Clicked += RequestCreate;

        var cancelButton = new UiButton("Cancel")
        {
            Width = 100.0f,
            Height = 34.0f
        };
        cancelButton.Clicked += Close;

        buttons.AddChild(createButton);
        buttons.AddChild(cancelButton);
        content.AddChild(buttons);

        SetContent(content);
    }

    public event Action<ProjectCreationRequest>? CreateRequested;

    public void ShowFor()
    {
        _nameBox.SetText("MyGame");
        _locationBox.SetText(GetDefaultLocation());
        SetMessage(string.Empty);
        UpdatePreview();
        Show(_nameBox);
    }

    /// <summary>
    /// Lets the application report validation or creation errors without
    /// coupling the dialog to a concrete project-generation implementation.
    /// </summary>
    public void SetMessage(string message)
    {
        _messageLabel.Text = message ?? string.Empty;
    }

    public void CloseAfterCreation()
    {
        Close();
    }

    private void BrowseForFolder()
    {
        try
        {
            var selectedPath = _fileDialogs.ShowSelectFolder(
                _locationBox.Text);

            if (selectedPath is null)
            {
                return;
            }

            _locationBox.SetText(selectedPath);
            SetMessage(string.Empty);
            UpdatePreview();
        }
        catch (Exception exception)
        {
            SetMessage(exception.Message);
        }
    }

    private void OnInputChanged(string _)
    {
        SetMessage(string.Empty);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var name = _nameBox.Text.Trim();
        var parentDirectory = _locationBox.Text.Trim();

        if (name.Length == 0 || parentDirectory.Length == 0)
        {
            _previewLabel.Text = "Project folder: —";
            return;
        }

        try
        {
            _previewLabel.Text =
                $"Project folder: {Path.GetFullPath(Path.Combine(parentDirectory, name))}";
        }
        catch
        {
            _previewLabel.Text = "Project folder: invalid path";
        }
    }

    private void RequestCreate()
    {
        if (!TryBuildRequest(out var request, out var error))
        {
            SetMessage(error);
            return;
        }

        if (CreateRequested is null)
        {
            SetMessage("Project creation is not connected to a project generator yet.");
            return;
        }

        CreateRequested.Invoke(request);
    }

    private bool TryBuildRequest(
        out ProjectCreationRequest request,
        out string error)
    {
        request = null!;
        error = string.Empty;

        var name = _nameBox.Text.Trim();
        if (name.Length == 0)
        {
            error = "Enter a project name.";
            return false;
        }

        if (name is "." or ".." ||
            name.EndsWith(' ') ||
            name.EndsWith('.') ||
            name.Any(character =>
                char.IsControl(character) ||
                Path.GetInvalidFileNameChars().Contains(character)))
        {
            error = "The project name contains characters that are not valid in a folder name.";
            return false;
        }

        var parentDirectory = _locationBox.Text.Trim();
        if (parentDirectory.Length == 0)
        {
            error = "Choose a parent folder for the project.";
            return false;
        }

        try
        {
            parentDirectory = Path.GetFullPath(parentDirectory);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            error = "The parent folder path is not valid.";
            return false;
        }

        if (!Directory.Exists(parentDirectory))
        {
            error = "The parent folder does not exist. Choose an existing folder.";
            return false;
        }

        var projectDirectory = Path.GetFullPath(
            Path.Combine(parentDirectory, name));

        if (Directory.Exists(projectDirectory) ||
            File.Exists(projectDirectory))
        {
            error = "A file or folder with this project name already exists in the selected location.";
            return false;
        }

        request = new ProjectCreationRequest(
            name,
            parentDirectory,
            projectDirectory);

        return true;
    }

    private static string GetDefaultLocation()
    {
        var documents = Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments);

        if (!string.IsNullOrWhiteSpace(documents) &&
            Directory.Exists(documents))
        {
            return documents;
        }

        var profile = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);

        return Directory.Exists(profile)
            ? profile
            : Environment.CurrentDirectory;
    }
}
