using Engine.Content;
using Engine.Core.Assets;

namespace Engine.Graphics.Resources;

public sealed class TextureResourceManager :
    ITextureResourceManager
{
    private readonly IContentManager _content;
    private readonly ITextureManager _textures;

    private readonly Dictionary<TextureAtlasKey, TextureAtlas> _atlases =
        new();

    private readonly Dictionary<
        (AssetPath Path, TextureColorSpace ColorSpace),
        TextureHandle> _loaded = new();

    private bool _disposed;

    public TextureResourceManager(
        IContentManager content,
        ITextureManager textures)
    {
        ArgumentNullException.ThrowIfNull(
            content);

        ArgumentNullException.ThrowIfNull(
            textures);

        _content =
            content;

        _textures =
            textures;
    }

    public TextureHandle Load(AssetPath path)
    {
        return Load(path, TextureColorSpace.Srgb);
    }

    public TextureHandle Load(
        AssetPath path,
        TextureColorSpace colorSpace)
    {
        EnsureNotDisposed();
        EnsurePathValid(path);

        if (!Enum.IsDefined(colorSpace))
            throw new ArgumentOutOfRangeException(nameof(colorSpace));

        var key = (path, colorSpace);

        if (TryGet(path, colorSpace, out var existing))
            return existing;

        var sourceData = _content.Load<TextureData>(path);

        var uploadData =
            sourceData.Description.ColorSpace == colorSpace
                ? sourceData
                : new TextureData(
                    sourceData.Description with
                    {
                        ColorSpace = colorSpace
                    },
                    sourceData.Pixels);

        TextureHandle texture;

        try
        {
            texture = _textures.Create(uploadData);

            if (!texture.IsValid || !_textures.Exists(texture))
            {
                throw new InvalidOperationException(
                    $"Texture manager returned an invalid texture handle " +
                    $"for asset '{path}'.");
            }
        }
        catch
        {
            _content.Unload<TextureData>(path);
            throw;
        }

        _loaded.Add(key, texture);

        return texture;
    }

    public bool IsLoaded(AssetPath path)
    {
        EnsureNotDisposed();
        return TryGet(path, out _);
    }

    public bool TryGetDescription(
        AssetPath path,
        out TextureDescription description)
    {
        EnsureNotDisposed();

        if (!TryGet(
                path,
                out var texture))
        {
            description =
                default;

            return false;
        }

        description =
            _textures.GetDescription(
                texture);

        return true;
    }

    private bool TryGet(
    AssetPath path,
    TextureColorSpace colorSpace,
    out TextureHandle texture)
    {
        var key = (path, colorSpace);

        if (_loaded.TryGetValue(key, out texture))
        {
            if (_textures.Exists(texture))
                return true;

            _loaded.Remove(key);
            _content.Unload<TextureData>(path);
        }

        texture = TextureHandle.Invalid;
        return false;
    }

    public bool TryGet(
    AssetPath path,
    out TextureHandle texture)
    {
        EnsureNotDisposed();

        if (TryGet(path, TextureColorSpace.Srgb, out texture))
            return true;

        var variants = _loaded.Keys
            .Where(key => key.Path == path)
            .Select(key => key.ColorSpace)
            .ToArray();

        foreach (var colorSpace in variants)
        {
            if (TryGet(path, colorSpace, out texture))
                return true;
        }

        texture = TextureHandle.Invalid;
        return false;
    }

    public TextureAtlas LoadAtlas(
        AssetPath path,
        int tileWidth,
        int tileHeight)
    {
        EnsureNotDisposed();
        EnsurePathValid(path);

        if (tileWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileWidth));
        }

        if (tileHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileHeight));
        }

        var key =
            new TextureAtlasKey(
                path,
                tileWidth,
                tileHeight);

        if (_atlases.TryGetValue(
                key,
                out var existing))
        {
            return existing;
        }

        var wasLoaded = TryGet(
            path,
            TextureColorSpace.Srgb,
            out _);

        var texture =
            Load(path);

        try
        {
            var description =
                _textures.GetDescription(texture);

            var atlas =
                new TextureAtlas(
                    texture,
                    description,
                    tileWidth,
                    tileHeight);

            _atlases.Add(
                key,
                atlas);

            return atlas;
        }
        catch
        {
            if (!wasLoaded)
            {
                UnloadVariant((path, TextureColorSpace.Srgb));
            }

            throw;
        }
    }

    private bool UnloadVariant(
    (AssetPath Path, TextureColorSpace ColorSpace) key)
    {
        if (!_loaded.Remove(key, out var texture))
            return false;

        if (_textures.Exists(texture))
            _textures.Destroy(texture);

        _content.Unload<TextureData>(key.Path);

        return true;
    }

    public bool Unload(AssetPath path)
    {
        EnsureNotDisposed();

        RemoveAtlases(path);

        var keys = _loaded.Keys
            .Where(key => key.Path == path)
            .ToArray();

        foreach (var key in keys)
            UnloadVariant(key);

        return keys.Length > 0;
    }

    public void UnloadAll()
    {
        EnsureNotDisposed();

        foreach (var key in _loaded.Keys.ToArray())
            UnloadVariant(key);

        _atlases.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var key in _loaded.Keys.ToArray())
            UnloadVariant(key);

        _atlases.Clear();

        _disposed = true;
    }

    private void RemoveAtlases(
        AssetPath path)
    {
        var keysToRemove =
            new List<TextureAtlasKey>();

        foreach (var key in _atlases.Keys)
        {
            if (key.Path == path)
            {
                keysToRemove.Add(
                    key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _atlases.Remove(
                key);
        }
    }

    private static void EnsurePathValid(
        AssetPath path)
    {
        if (string.IsNullOrWhiteSpace(
                path.Value))
        {
            throw new ArgumentException(
                "Asset path must be valid.",
                nameof(path));
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}