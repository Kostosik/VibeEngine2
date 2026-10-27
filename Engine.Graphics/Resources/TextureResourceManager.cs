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

    private readonly Dictionary<AssetPath, TextureHandle> _loaded =
        new();

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

    public TextureHandle Load(
        AssetPath path)
    {
        EnsureNotDisposed();
        EnsurePathValid(path);

        if (_loaded.TryGetValue(
                path,
                out var existing))
        {
            if (_textures.Exists(
                    existing))
            {
                return existing;
            }

            _loaded.Remove(
                path);
        }

        var data =
            _content.Load<TextureData>(
                path);

        TextureHandle texture;

        try
        {
            texture = _textures.Create(data);

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

        return _textures.Exists(
            texture);
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

    public bool TryGet(
        AssetPath path,
        out TextureHandle texture)
    {
        EnsureNotDisposed();

        if (_loaded.TryGetValue(
                path,
                out texture))
        {
            if (_textures.Exists(
                    texture))
            {
                return true;
            }

            _loaded.Remove(
                path);
        }

        texture =
            TextureHandle.Invalid;

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

        var wasLoaded =
            TryGet(path, out _);

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
                Unload(path);
            }

            throw;
        }
    }

    public bool Unload(
        AssetPath path)
    {
        EnsureNotDisposed();

        RemoveAtlases(
            path);

        if (!_loaded.Remove(
                path,
                out var texture))
        {
            return false;
        }

        if (_textures.Exists(
                texture))
        {
            _textures.Destroy(
                texture);
        }

        _content.Unload<TextureData>(
            path);

        return true;
    }

    public void UnloadAll()
    {
        EnsureNotDisposed();

        _atlases.Clear();

        foreach (var pair in _loaded)
        {
            if (_textures.Exists(
                    pair.Value))
            {
                _textures.Destroy(
                    pair.Value);
            }

            _content.Unload<TextureData>(
                pair.Key);
        }

        _loaded.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _atlases.Clear();

        foreach (var pair in _loaded)
        {
            if (_textures.Exists(
                    pair.Value))
            {
                _textures.Destroy(
                    pair.Value);
            }

            _content.Unload<TextureData>(
                pair.Key);
        }

        _loaded.Clear();

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