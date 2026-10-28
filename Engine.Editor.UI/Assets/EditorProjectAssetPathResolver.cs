using Engine.Core.Assets;

namespace Engine.Editor.UI.Assets;

internal static class EditorProjectAssetPathResolver
{
    public static string Resolve(
        string assetsRoot,
        string assetPath,
        string assetDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetsRoot);
        ArgumentNullException.ThrowIfNull(assetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDescription);

        if (Path.IsPathRooted(assetPath))
        {
            throw new InvalidDataException(
                $"{assetDescription} must use a relative path inside Assets.");
        }

        var normalizedPath =
            new AssetPath(assetPath).Value;

        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            throw new InvalidDataException(
                $"{assetDescription} path is empty.");
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(assetsRoot, normalizedPath));

        if (!IsWithinRoot(fullPath, assetsRoot))
        {
            throw new InvalidDataException(
                $"{assetDescription} path cannot point outside Assets.");
        }

        return fullPath;
    }

    private static bool IsOutsideRoot(string relativePath)
    {
        return Path.IsPathRooted(relativePath) ||
               relativePath.Equals(
                   "..",
                   StringComparison.Ordinal) ||
               relativePath.StartsWith(
                   $"..{Path.DirectorySeparatorChar}",
                   StringComparison.Ordinal) ||
               relativePath.StartsWith(
                   $"..{Path.AltDirectorySeparatorChar}",
                   StringComparison.Ordinal);
    }

    public static bool IsWithinRoot(
    string path,
    string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(path);

        var relativePath = Path.GetRelativePath(
            fullRoot,
            fullPath);

        return !IsOutsideRoot(relativePath);
    }
}