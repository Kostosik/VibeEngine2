using Engine.Content.Assets;
using Engine.Core.Assets;

namespace Engine.Content.HotReload;

public sealed class ContentHotReloadService
{
    private readonly IContentCatalog _catalog;
    private readonly IContentManager _content;

    private readonly Dictionary<
        AssetPath,
        ContentAsset> _knownAssets =
        new();

    public ContentHotReloadService(
        IContentCatalog catalog,
        IContentManager content)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        ArgumentNullException.ThrowIfNull(
            content);

        _catalog = catalog;
        _content = content;

        RefreshSnapshot();
    }

    public IReadOnlyList<AssetPath> Update()
    {
        _catalog.Refresh();

        var changed =
            new List<AssetPath>();

        foreach (var asset in
                 _catalog.GetAssets())
        {
            if (!_knownAssets.TryGetValue(
                    asset.Path,
                    out var previous))
            {
                _knownAssets[asset.Path] =
                    asset;

                continue;
            }

            if (HasChanged(
                    previous,
                    asset))
            {
                changed.Add(
                    asset.Path);

                _knownAssets[asset.Path] =
                    asset;
            }
        }

        var removed =
            _knownAssets.Keys
                .Where(
                    path => !_catalog.Contains(path))
                .ToArray();

        foreach (var path in removed)
        {
            _knownAssets.Remove(
                path);
        }

        return changed;
    }

    public bool Reload<T>(
        AssetPath path)
        where T : class
    {
        if (!_catalog.Contains(path))
        {
            return false;
        }

        if (!_content.IsLoaded<T>(path))
        {
            return false;
        }

        _content.Reload<T>(
            path);

        return true;
    }

    public void RefreshSnapshot()
    {
        _catalog.Refresh();

        _knownAssets.Clear();

        foreach (var asset in
                 _catalog.GetAssets())
        {
            _knownAssets.Add(
                asset.Path,
                asset);
        }
    }

    private static bool HasChanged(
        ContentAsset previous,
        ContentAsset current)
    {
        return previous.SizeBytes !=
                   current.SizeBytes ||
               previous.LastModifiedUtc !=
                   current.LastModifiedUtc ||
               !string.Equals(
                   previous.Extension,
                   current.Extension,
                   StringComparison.OrdinalIgnoreCase);
    }
}