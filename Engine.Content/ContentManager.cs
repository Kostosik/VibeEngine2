using Engine.Core.Assets;
using Engine.Content.Assets;
using Engine.Content.Loading;

namespace Engine.Content;

public sealed class ContentManager :
    IContentManager,
    IContentLoadContext,
    IDisposable
{
    private readonly IAssetSource _source;
    private readonly IContentCatalog _catalog;
    private readonly IContentLoaderRegistry _loaders;

    private readonly HashSet<ContentLoadKey> _loading =
    new();

    private readonly Dictionary<
        Type,
        Dictionary<AssetPath, object>> _cache =
        new();

    private bool _disposed;

    public ContentManager(
        IAssetSource source,
        IContentCatalog catalog,
        IContentLoaderRegistry loaders)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        ArgumentNullException.ThrowIfNull(
            catalog);

        ArgumentNullException.ThrowIfNull(
            loaders);

        _source = source;
        _catalog = catalog;
        _loaders = loaders;
    }

    public void Register<T>(
        IContentLoader<T> loader)
        where T : class
    {
        EnsureNotDisposed();

        _loaders.Register(
            loader);
    }

    public T Load<T>(
        AssetPath path)
        where T : class
    {
        EnsureNotDisposed();

        if (!_catalog.TryGet(
         path,
         out var contentAsset) ||
     contentAsset is null)
        {
            throw new FileNotFoundException(
                $"Content asset '{path}' is not registered.");
        }

        var type =
            typeof(T);

        if (!_loaders.TryGet<T>(
                contentAsset,
                out var loader))
        {
            throw new InvalidOperationException(
                $"No content loader is registered for asset '{path}' as '{type.Name}'.");
        }

        if (!_cache.TryGetValue(
                type,
                out var typeCache))
        {
            typeCache =
                new Dictionary<AssetPath, object>();

            _cache.Add(
                type,
                typeCache);
        }

        if (typeCache.TryGetValue(
                path,
                out var existing))
        {
            return (T)existing;
        }

        var loadKey =
    new ContentLoadKey(
        type,
        path);

        if (!_loading.Add(
                loadKey))
        {
            throw new InvalidOperationException(
                $"Cyclic content dependency detected for '{type.Name}' at '{path}'.");
        }

        try
        {
            var asset =
                loader!.Load(
                    path,
                    this);

            ArgumentNullException.ThrowIfNull(
                asset);

            typeCache.Add(
                path,
                asset);

            return asset;
        }
        finally
        {
            _loading.Remove(
                loadKey);
        }
    }



    public bool IsLoaded<T>(
        AssetPath path)
        where T : class
    {
        EnsureNotDisposed();

        return _cache.TryGetValue(
                   typeof(T),
                   out var typeCache) &&
               typeCache.ContainsKey(
                   path);
    }

    public ReadOnlyMemory<byte> ReadBytes(
        AssetPath path)
    {
        EnsureNotDisposed();

        return _source.Load(
            path);
    }

    public void ClearCache()
    {
        EnsureNotDisposed();

        foreach (var typeCache in _cache.Values)
        {
            foreach (var asset in typeCache.Values)
            {
                if (asset is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        _cache.Clear();
        _loading.Clear();
    }

    public bool Unload<T>(
    AssetPath path)
    where T : class
    {
        EnsureNotDisposed();

        if (!_cache.TryGetValue(
                typeof(T),
                out var typeCache))
        {
            return false;
        }

        if (!typeCache.Remove(
                path,
                out var asset))
        {
            return false;
        }

        if (asset is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (typeCache.Count == 0)
        {
            _cache.Remove(
                typeof(T));
        }

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearCache();
        _loaders.Clear();

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private readonly record struct ContentLoadKey(
    Type Type,
    AssetPath Path);
}