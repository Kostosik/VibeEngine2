namespace Engine.Editor.Assets;

public enum EditorAssetBrowserSortDirection
{
    Ascending,
    Descending
}


public sealed class EditorAssetBrowserViewState
{
    public EditorAssetKind? KindFilter { get; private set; }

    public void SetKindFilter(EditorAssetKind? kind)
    {
        KindFilter = kind;
    }

    public string SearchText { get; private set; } =
        string.Empty;

    public EditorAssetBrowserSortDirection SortDirection
    {
        get;
        private set;
    } = EditorAssetBrowserSortDirection.Ascending;

    public void SetSearchText(string? text)
    {
        SearchText = text?.Trim() ?? string.Empty;
    }

    public void ToggleSortDirection()
    {
        SortDirection =
            SortDirection == EditorAssetBrowserSortDirection.Ascending
                ? EditorAssetBrowserSortDirection.Descending
                : EditorAssetBrowserSortDirection.Ascending;
    }

    public IReadOnlyList<EditorAssetEntry> GetVisibleEntries(
        IEnumerable<EditorAssetEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var filtered = entries.Where(
            entry =>
                (SearchText.Length == 0 ||
                 entry.Name.Contains(
                     SearchText,
                     StringComparison.OrdinalIgnoreCase))
                &&
                (
                    entry.IsDirectory ||
                    KindFilter is null ||
                    EditorAssetTypeClassifier.Classify(entry) == KindFilter
                ));

        // Папки всегда располагаются раньше файлов.
        var grouped = filtered.OrderBy(
            entry => entry.IsDirectory ? 0 : 1);

        IOrderedEnumerable<EditorAssetEntry> ordered;

        if (SortDirection == EditorAssetBrowserSortDirection.Ascending)
        {
            ordered = grouped
                .ThenBy(
                    entry => entry.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    entry => entry.Path,
                    StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            ordered = grouped
                .ThenByDescending(
                    entry => entry.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(
                    entry => entry.Path,
                    StringComparer.OrdinalIgnoreCase);
        }

        return ordered.ToArray();
    }
}