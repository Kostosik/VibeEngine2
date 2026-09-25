namespace Engine.Editor.Assets;

public sealed class FileSystemAssetSource :
    IEditorAssetSource
{
    public IReadOnlyList<EditorAssetEntry> GetEntries(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"Asset directory '{path}' was not found.");
        }

        var directory =
            new DirectoryInfo(path);

        var entries =
            new List<EditorAssetEntry>();

        foreach (var childDirectory
                 in directory.GetDirectories())
        {
            entries.Add(
                new EditorAssetEntry(
                    childDirectory.FullName,
                    childDirectory.Name,
                    true));
        }

        foreach (var file
                 in directory.GetFiles())
        {
            entries.Add(
                new EditorAssetEntry(
                    file.FullName,
                    file.Name,
                    false));
        }

        entries.Sort(
            static (left, right) =>
            {
                var directoryComparison =
                    right.IsDirectory.CompareTo(
                        left.IsDirectory);

                if (directoryComparison != 0)
                {
                    return directoryComparison;
                }

                return StringComparer.OrdinalIgnoreCase.Compare(
                    left.Name,
                    right.Name);
            });

        return entries;
    }
}