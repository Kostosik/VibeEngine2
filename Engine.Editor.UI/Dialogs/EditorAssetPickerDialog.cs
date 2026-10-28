using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Editor.UI.Assets;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Dialogs;

public sealed class EditorAssetPickerDialog
{
    private readonly EditorContext _editor;
    private readonly UiModal _modal;

    private readonly UiLabel _titleLabel;
    private readonly UiLabel _locationLabel;
    private readonly UiLabel _emptyLabel;

    private readonly UiButton _upButton;
    private readonly UiButton _cancelButton;

    private readonly UiStackPanel _entriesPanel;

    private Func<EditorAssetEntry, bool>? _canSelect;
    private Action<string>? _selected;

    private string _rootPath = string.Empty;
    private string _currentPath = string.Empty;

    public EditorAssetPickerDialog(
        EditorContext editor,
        UiOverlayLayer overlays,
        UiFocusManager focus)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(overlays);
        ArgumentNullException.ThrowIfNull(focus);

        _editor = editor;

        _modal = new UiModal(
            overlays,
            focus);

        _modal.SetSize(
            900.0f,
            600.0f);

        _titleLabel = new UiLabel
        {
            Height = 28.0f,
            FontSize = 18.0f
        };

        _locationLabel = new UiLabel
        {
            Width = 780.0f,
            Height = 32.0f,
            FontSize = 12.0f,
            VerticalAlignment =
                UiVerticalAlignment.Center
        };

        _upButton = new UiButton("Up")
        {
            Width = 64.0f,
            Height = 32.0f,
            FontSize = 12.0f
        };

        _upButton.Clicked += NavigateUp;

        var pathRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 32.0f,
            HorizontalAlignment =
                UiHorizontalAlignment.Stretch
        };

        pathRow.AddChild(_upButton);
        pathRow.AddChild(_locationLabel);

        _entriesPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 4.0f,
            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,
            VerticalAlignment =
                UiVerticalAlignment.Top
        };

        var scrollView = new UiScrollView
        {
            Height = 420.0f,
            VerticalScrolling = true,
            HorizontalScrolling = false,
            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,
            VerticalAlignment =
                UiVerticalAlignment.Stretch
        };

        scrollView.SetContent(_entriesPanel);

        _emptyLabel = new UiLabel(
            "No matching assets in this folder")
        {
            Height = 24.0f,
            FontSize = 12.0f,
            Color = new UiColor(150, 150, 150, 255)
        };

        _cancelButton = new UiButton("Cancel")
        {
            Width = 90.0f,
            Height = 32.0f
        };

        _cancelButton.Clicked += _modal.Close;

        var footer = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Height = 32.0f,
            HorizontalAlignment =
                UiHorizontalAlignment.Right
        };

        footer.AddChild(_cancelButton);

        var content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            Width = 860.0f,
            Height = 560.0f,
            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,
            VerticalAlignment =
                UiVerticalAlignment.Stretch
        };

        content.AddChild(_titleLabel);
        content.AddChild(pathRow);
        content.AddChild(scrollView);
        content.AddChild(_emptyLabel);
        content.AddChild(footer);

        _modal.SetContent(content);

        _modal.Closed += ClearRequest;
    }

    public bool IsOpen => _modal.IsOpen;

    public bool Show(
        string title,
        Func<EditorAssetEntry, bool> canSelect,
        Action<string> selected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(canSelect);
        ArgumentNullException.ThrowIfNull(selected);

        if (_modal.IsOpen)
        {
            _modal.Close();
        }

        var browser = _editor.AssetBrowser;

        if (browser is null)
        {
            return false;
        }

        _rootPath = browser.RootPath;
        _currentPath = browser.CurrentPath;

        if (!Directory.Exists(_currentPath) ||
            !EditorProjectAssetPathResolver.IsWithinRoot(
    _currentPath,
    _rootPath))
        {
            _currentPath = _rootPath;
        }

        _titleLabel.Text = title;
        _canSelect = canSelect;
        _selected = selected;

        RefreshEntries();

        _modal.Show(_cancelButton);

        return true;
    }

    public void Close()
    {
        _modal.Close();
    }

    private void RefreshEntries()
    {
        _entriesPanel.ClearChildren();

        var browser = _editor.AssetBrowser;

        var entries = browser is null
            ? Array.Empty<EditorAssetEntry>()
            : browser.GetEntriesAtPath(_currentPath)
                .Where(entry =>
                    entry.IsDirectory ||
                    _canSelect?.Invoke(entry) == true)
                .OrderByDescending(
                    entry => entry.IsDirectory)
                .ThenBy(
                    entry => entry.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var relativePath = Path.GetRelativePath(
            _rootPath,
            _currentPath);

        _locationLabel.Text =
            relativePath == "."
                ? "Assets"
                : $"Assets/{relativePath.Replace('\\', '/')}";

        _upButton.Enabled =
            !string.Equals(
                Path.GetFullPath(_currentPath),
                Path.GetFullPath(_rootPath),
                StringComparison.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var text = entry.IsDirectory
                ? $"[Folder] {entry.Name}"
                : entry.Name;

            var button = new UiButton(text)
            {
                Width = 820.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                HorizontalTextAlignment =
                    UiTextHorizontalAlignment.Left
            };

            button.Clicked += () =>
            {
                if (entry.IsDirectory)
                {
                    _currentPath = entry.Path;
                    RefreshEntries();
                    return;
                }

                SelectEntry(entry);
            };

            _entriesPanel.AddChild(button);
        }

        _emptyLabel.Visible =
            entries.Length == 0;
    }

    private void NavigateUp()
    {
        if (!_upButton.Enabled)
        {
            return;
        }

        var parent = Directory.GetParent(_currentPath);

        if (parent is null ||
            !EditorProjectAssetPathResolver.IsWithinRoot(
    parent.FullName,
    _rootPath))
        {
            return;
        }

        _currentPath = parent.FullName;

        RefreshEntries();
    }

    private void SelectEntry(
        EditorAssetEntry entry)
    {
        if (entry.IsDirectory ||
            _canSelect?.Invoke(entry) != true)
        {
            return;
        }

        var relativePath = Path.GetRelativePath(
            _rootPath,
            entry.Path)
            .Replace('\\', '/');

        var callback = _selected;

        _modal.Close();

        callback?.Invoke(relativePath);
    }

    private void ClearRequest()
    {
        _canSelect = null;
        _selected = null;
    }
}