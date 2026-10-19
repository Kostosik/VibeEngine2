using Engine.Editor.Selection;

namespace Engine.Editor.Assets;

public sealed class EditorAssetBrowser
{
    private readonly IEditorAssetSource _source;
    private readonly List<string> _navigationHistory = new();
    private int _navigationHistoryIndex;
    public event Action? Changed;
    public EditorAssetBrowser(
        IEditorAssetSource source,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException(
                $"Asset root '{rootPath}' was not found.");
        }

        _source = source;
        RootPath = Path.GetFullPath(rootPath);
        CurrentPath = RootPath;
        _navigationHistory.Add(CurrentPath);
        Selection =
            new SelectionSet<string>();

        Refresh();
    }

    public string RootPath { get; }

    public string CurrentPath { get; private set; }
    public bool CanNavigateBack =>
    _navigationHistoryIndex > 0;

    public bool CanNavigateForward =>
        _navigationHistoryIndex < _navigationHistory.Count - 1;
    public IReadOnlyList<EditorAssetEntry> Entries { get; private set; } =
        Array.Empty<EditorAssetEntry>();

    public SelectionSet<string> Selection { get; }

    public EditorAssetEntry? SelectedEntry
    {
        get
        {
            var path = Selection.Items.FirstOrDefault();

            if (path is null)
            {
                return null;
            }

            return Entries.FirstOrDefault(
                entry => string.Equals(
                    entry.Path,
                    path,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    public EditorAssetEntry? SelectedAsset =>
        SelectedEntry is { IsDirectory: false } entry
            ? entry
            : null;

    public void Refresh()
    {
        var changed =
            RefreshCore();

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public bool NavigateTo(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (!IsWithinRoot(fullPath) ||
            !Directory.Exists(fullPath))
        {
            return false;
        }

        if (string.Equals(
                CurrentPath,
                fullPath,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Новый маршрут удаляет будущие записи истории.
        if (_navigationHistoryIndex < _navigationHistory.Count - 1)
        {
            _navigationHistory.RemoveRange(
                _navigationHistoryIndex + 1,
                _navigationHistory.Count - _navigationHistoryIndex - 1);
        }

        _navigationHistory.Add(fullPath);
        _navigationHistoryIndex = _navigationHistory.Count - 1;

        SetCurrentPath(fullPath);

        return true;
    }

    public bool NavigateBack() =>
    NavigateHistory(-1);

    public bool NavigateForward() =>
        NavigateHistory(1);

    private bool NavigateHistory(int direction)
    {
        var targetIndex = _navigationHistoryIndex + direction;

        while (targetIndex >= 0 &&
               targetIndex < _navigationHistory.Count)
        {
            var targetPath = _navigationHistory[targetIndex];

            // Папка могла быть удалена после посещения.
            if (!IsWithinRoot(targetPath) ||
                !Directory.Exists(targetPath))
            {
                _navigationHistory.RemoveAt(targetIndex);

                if (targetIndex < _navigationHistoryIndex)
                {
                    _navigationHistoryIndex--;
                }

                targetIndex = _navigationHistoryIndex + direction;
                continue;
            }

            _navigationHistoryIndex = targetIndex;
            SetCurrentPath(targetPath);

            return true;
        }

        return false;
    }

    private void SetCurrentPath(string path)
    {
        CurrentPath = path;

        Selection.Clear();

        RefreshCore();

        Changed?.Invoke();
    }

    public bool NavigateUp()
    {
        if (string.Equals(
                CurrentPath,
                RootPath,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parent =
            Directory.GetParent(
                CurrentPath);

        if (parent is null)
        {
            return false;
        }

        return NavigateTo(
            parent.FullName);
    }

    public bool Select(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var entry = Entries.FirstOrDefault(
            candidate => string.Equals(
                candidate.Path,
                path,
                StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            return false;
        }

        var alreadySelected =
            Selection.Count == 1 &&
            Selection.Contains(entry.Path);

        Selection.Set(entry.Path);

        if (!alreadySelected)
        {
            Changed?.Invoke();
        }

        return true;
    }

    private bool RefreshCore()
    {
        var entries =
            _source.GetEntries(
                CurrentPath);

        var entriesChanged =
            !Entries.SequenceEqual(
                entries);

        var selected =
            Selection.Items.ToArray();

        var selectionChanged = false;

        foreach (var path in selected)
        {
            if (entries.Any(
                    entry =>
                        string.Equals(
                            entry.Path,
                            path,
                            StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (Selection.Remove(path))
            {
                selectionChanged = true;
            }
        }

        Entries =
            entries;

        return entriesChanged ||
               selectionChanged;
    }

    private bool IsWithinRoot(
        string path)
    {
        var root =
            RootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return string.Equals(
                   path,
                   RootPath,
                   StringComparison.OrdinalIgnoreCase)
               ||
               path.StartsWith(
                   root,
                   StringComparison.OrdinalIgnoreCase);
    }
}