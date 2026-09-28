using Engine.Core.Assets;

namespace Engine.Content.Assets;

public sealed class FileContentCatalog :
    IContentCatalog
{
    private readonly string _rootDirectory;

    private readonly Dictionary<
        AssetPath,
        ContentAsset> _assets =
        new();

    public FileContentCatalog(
        string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            rootDirectory);

        _rootDirectory =
            Path.GetFullPath(
                rootDirectory);

        if (!Directory.Exists(
                _rootDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Content root '{_rootDirectory}' was not found.");
        }

        Refresh();
    }

    public IReadOnlyList<ContentAsset> GetAssets()
    {
        return _assets
            .Values
            .OrderBy(
                asset => asset.Path.Value,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool Contains(
        AssetPath path)
    {
        return _assets.ContainsKey(
            path);
    }

    public bool TryGet(
        AssetPath path,
        out ContentAsset? asset)
    {
        return _assets.TryGetValue(
            path,
            out asset);
    }

    public void Refresh()
    {
        _assets.Clear();

        foreach (var file in
                 Directory.EnumerateFiles(
                     _rootDirectory,
                     "*",
                     SearchOption.AllDirectories))
        {
            var relativePath =
                Path.GetRelativePath(
                    _rootDirectory,
                    file);

            var assetPath =
                new AssetPath(
                    relativePath);

            var fileInfo =
                new FileInfo(
                    file);

            _assets.Add(
                assetPath,
                new ContentAsset(
                    assetPath,
                    fileInfo.Extension,
                    fileInfo.Length));
        }
    }
}