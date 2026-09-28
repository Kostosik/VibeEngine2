using Engine.Core.Assets;
using Engine.Content.Assets;
using Engine.Content.Loading;

namespace Engine.Content;

public sealed class ContentManager :
    IContentLoadContext,
    IDisposable
{
    private readonly IAssetSource _source;
    private readonly IContentCatalog _catalog;

    private readonly Dictionary<
        Type,
        Dictionary<AssetPath, object>> _cache =
        new();

    private readonly Dictionary<
        Type,
        object> _loaders =
        new();

    private bool _disposed;

    public ContentManager(
        IAssetSource source,
        IContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        ArgumentNullException.ThrowIfNull(
            catalog);

        _source = source;
        _catalog = catalog;
    }

    public void Register<T>(
        IContentLoader<T> loader)
        where T : class
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            loader);

        var type =
            typeof(T);

        if (!_loaders.TryAdd(
                type,
                loader))
        {
            throw new InvalidOperationException(
                $"A content loader for '{type.Name}' " +
                "is already registered.");
        }
    }

    public T Load<T>(
        AssetPath path)
        where T : class
    {
        EnsureNotDisposed();

        if (!_catalog.Contains(
                path))
        {
            throw new FileNotFoundException(
                $"Content asset '{path}' is not registered.");
        }

        var type =
            typeof(T);

        if (!_loaders.TryGetValue(
                type,
                out var loaderObject))
        {
            throw new InvalidOperationException(
                $"No content loader is registered for '{type.Name}'.");
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

        var loader =
            (IContentLoader<T>)loaderObject;

        var asset =
            loader.Load(
                path,
                this);

        ArgumentNullException.ThrowIfNull(
            asset);

        typeCache.Add(
            path,
            asset);

        return asset;
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

        _cache.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cache.Clear();
        _loaders.Clear();

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}