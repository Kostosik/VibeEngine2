using Engine.Core.Assets;

namespace Engine.Graphics.Resources;

public sealed class TextureResourceManager
    : ITextureResourceManager
{
    private readonly IAssetSource _assets;
    private readonly ITextureManager _textures;
    private readonly ImageTextureLoader _loader;

    private readonly Dictionary<TextureAtlasKey, TextureAtlas> _atlases =
    new();

    private readonly Dictionary<AssetPath, TextureHandle> _loaded =
        new();

    private bool _disposed;

    public TextureResourceManager(
        IAssetSource assets,
        ITextureManager textures)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(textures);

        _assets = assets;
        _textures = textures;

        _loader = new ImageTextureLoader();
    }

    public TextureHandle Load(
        AssetPath path)
    {
        EnsureNotDisposed();
        EnsurePathValid(path);

        if (_loaded.TryGetValue(
                path,
                out var existing))
        {
            if (_textures.Exists(existing))
                return existing;

            _loaded.Remove(path);
        }

        var data =
            _loader.Load(
                _assets,
                path);

        var texture =
            _textures.Create(data);

        _loaded.Add(
            path,
            texture);

        return texture;
    }

    public bool IsLoaded(
        AssetPath path)
    {
        EnsureNotDisposed();

        if (!_loaded.TryGetValue(
                path,
                out var texture))
        {
            return false;
        }

        return _textures.Exists(texture);
    }

    public bool TryGet(
        AssetPath path,
        out TextureHandle texture)
    {
        EnsureNotDisposed();

        if (_loaded.TryGetValue(
                path,
                out texture))
        {
            if (_textures.Exists(texture))
                return true;

            _loaded.Remove(path);
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

        var texture =
            Load(path);

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

    private void RemoveAtlases(
    AssetPath path)
    {
        var keysToRemove =
            new List<TextureAtlasKey>();

        foreach (var key in _atlases.Keys)
        {
            if (key.Path == path)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _atlases.Remove(key);
        }
    }

    public bool Unload(
        AssetPath path)
    {
        EnsureNotDisposed();

        RemoveAtlases(path);

        if (!_loaded.Remove(
                path,
                out var texture))
        {
            return false;
        }

        if (_textures.Exists(texture))
        {
            _textures.Destroy(texture);
        }

        return true;
    }

    public void UnloadAll()
    {
        EnsureNotDisposed();

        _atlases.Clear();

        foreach (var texture in _loaded.Values)
        {
            if (_textures.Exists(texture))
            {
                _textures.Destroy(texture);
            }
        }

        _loaded.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _atlases.Clear();

        foreach (var texture in _loaded.Values)
        {
            if (_textures.Exists(texture))
            {
                _textures.Destroy(texture);
            }
        }

        _loaded.Clear();

        _disposed = true;
    }

    private static void EnsurePathValid(
        AssetPath path)
    {
        if (string.IsNullOrWhiteSpace(path.Value))
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