using Engine.Core.Assets;
using Engine.Graphics.Resources;

namespace Engine.Editor.UI.Workspace;

internal sealed class ViewportProjectTextureCache
{
    private readonly ITextureManager? _textureManager;
    private readonly ImageTextureLoader _imageLoader = new();

    private readonly Dictionary<string, TextureHandle> _textures =
        new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

    private readonly HashSet<string> _unavailableTexturePaths =
        new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

    private string? _assetsRoot;

    public bool IsAvailable =>
        _textureManager is not null;

    public bool HasAssetsRoot =>
        _assetsRoot is not null;

    public ViewportProjectTextureCache(
        ITextureManager? textureManager)
    {
        _textureManager = textureManager;
    }

    public void SetAssetsRoot(string rootPath)
    {
        var fullRoot = Path.GetFullPath(rootPath);

        if (_assetsRoot is not null &&
            string.Equals(
                _assetsRoot,
                fullRoot,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            return;
        }

        ReleaseCachedTextures();

        _assetsRoot = fullRoot;
    }

    public bool TryLoad(
        string texturePath,
        out TextureHandle texture)
    {
        texture = default;

        if (_textureManager is null ||
            _assetsRoot is null)
        {
            return false;
        }

        if (_unavailableTexturePaths.Contains(texturePath))
        {
            return false;
        }

        try
        {
            var fullPath = ResolveTexturePath(
                _assetsRoot,
                texturePath);

            if (_textures.TryGetValue(
                    fullPath,
                    out texture) &&
                _textureManager.Exists(texture))
            {
                return true;
            }

            var imageData = _imageLoader.Load(
                File.ReadAllBytes(fullPath));

            texture = _textureManager.Create(imageData);

            if (!texture.IsValid ||
                !_textureManager.Exists(texture))
            {
                throw new InvalidOperationException(
                    "Graphics backend failed to create the sprite texture.");
            }

            _textures[fullPath] = texture;

            return true;
        }
        catch
        {
            _unavailableTexturePaths.Add(texturePath);

            texture = default;

            return false;
        }
    }

    private static string ResolveTexturePath(
        string assetsRoot,
        string texturePath)
    {
        if (Path.IsPathRooted(texturePath))
        {
            throw new InvalidDataException(
                "Sprite texture paths must be relative to the project's Assets directory.");
        }

        var fullRoot = Path.GetFullPath(assetsRoot);

        var relativeAssetPath = new AssetPath(texturePath).Value;

        if (string.IsNullOrWhiteSpace(relativeAssetPath))
        {
            throw new InvalidDataException(
                "Sprite texture path is empty.");
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(fullRoot, relativeAssetPath));

        var relativePath = Path.GetRelativePath(
            fullRoot,
            fullPath);

        if (Path.IsPathRooted(relativePath) ||
            relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Sprite texture path cannot point outside the project's Assets directory.");
        }

        return fullPath;
    }

    public void ReleaseResources()
    {
        ReleaseCachedTextures();
        _assetsRoot = null;
    }

    private void ReleaseCachedTextures()
    {
        if (_textureManager is not null)
        {
            foreach (var texture in _textures.Values)
            {
                if (_textureManager.Exists(texture))
                {
                    _textureManager.Destroy(texture);
                }
            }
        }

        _textures.Clear();
        _unavailableTexturePaths.Clear();
    }
}