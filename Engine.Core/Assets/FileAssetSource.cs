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

        var fullPath = ResolveAssetPath(path);

        var relativePath =
            Path.GetRelativePath(
                _rootDirectory,
                fullPath);

        if (Path.IsPathRooted(relativePath) ||
            relativePath == ".." ||
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

        var fullPath = ResolveAssetPath(path);

        var relativePath =
            Path.GetRelativePath(
                _rootDirectory,
                fullPath);

        if (Path.IsPathRooted(relativePath) ||
            relativePath == ".." ||
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

    private void EnsureAssetRootIsNotReparsePoint()
    {
        if (!Directory.Exists(_rootDirectory))
        {
            return;
        }

        var attributes = File.GetAttributes(_rootDirectory);

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Asset root '{_rootDirectory}' must not be a symbolic link or reparse point.");
        }
    }

    private string ResolveAssetPath(AssetPath path)
    {
        EnsureAssetRootIsNotReparsePoint();
        if (string.IsNullOrWhiteSpace(path.Value))
        {
            throw new ArgumentException(
                "Asset path is empty.",
                nameof(path));
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(_rootDirectory, path.Value));

        var relativePath = Path.GetRelativePath(
            _rootDirectory,
            fullPath);

        if (Path.IsPathRooted(relativePath) ||
            relativePath == ".." ||
            relativePath.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal) ||
            relativePath.StartsWith(
                ".." + Path.AltDirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Asset path '{path}' escapes the asset root.");
        }

        var currentPath = _rootDirectory;

        var segments = relativePath.Split(
            new[]
            {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
            },
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            currentPath = Path.Combine(
                currentPath,
                segment);

            if (!File.Exists(currentPath) &&
                !Directory.Exists(currentPath))
            {
                break;
            }

            var attributes = File.GetAttributes(currentPath);

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Asset path '{path}' traverses a symbolic link " +
                    "or reparse point.");
            }
        }

        return fullPath;
    }
}