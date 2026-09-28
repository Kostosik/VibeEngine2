namespace Engine.Core.Assets;

public sealed class FileAssetSource :
    IAssetSource,
    IAsyncAssetSource
{
    private readonly string _rootDirectory;

    public FileAssetSource(
        string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            rootDirectory);

        _rootDirectory =
            Path.GetFullPath(
                rootDirectory);
    }

    public ReadOnlyMemory<byte> Load(
        AssetPath path)
    {
        if (string.IsNullOrWhiteSpace(path.Value))
        {
            throw new ArgumentException(
                "Asset path is empty.",
                nameof(path));
        }

        var fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _rootDirectory,
                    path.Value));

        var relativePath =
            Path.GetRelativePath(
                _rootDirectory,
                fullPath);

        if (relativePath == ".." ||
            relativePath.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Asset path '{path}' escapes the asset root.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Asset '{path}' was not found.",
                fullPath);
        }

        return File.ReadAllBytes(
            fullPath);
    }

    public async ValueTask<ReadOnlyMemory<byte>> LoadAsync(
    AssetPath path,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path.Value))
        {
            throw new ArgumentException(
                "Asset path is empty.",
                nameof(path));
        }

        var fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _rootDirectory,
                    path.Value));

        var relativePath =
            Path.GetRelativePath(
                _rootDirectory,
                fullPath);

        if (relativePath == ".." ||
            relativePath.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Asset path '{path}' escapes the asset root.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Asset '{path}' was not found.",
                fullPath);
        }

        var data =
            await File.ReadAllBytesAsync(
                fullPath,
                cancellationToken);

        return data;
    }
}