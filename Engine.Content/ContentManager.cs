using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;

namespace Engine.Content;

public sealed class ContentManager :
    IContentManager,
    IContentLoadContext,
    IDisposable
{
    private readonly IAssetSource _source;
    private readonly IContentCatalog _catalog;
    private readonly IContentLoaderRegistry _loaders;
    private readonly AsyncLocal<Stack<ContentLoadFrame>?> _asyncLoading =
    new();

    private readonly Dictionary<
        Type,
        Dictionary<AssetPath, ContentEntry>> _cache =
        new();

    private readonly object _cacheSync =
    new();

    private readonly
        System.Collections.Concurrent.ConcurrentDictionary<
            ContentKey,
            Lazy<Task<ContentEntry>>> _inFlightLoads =
            new();

    private readonly Dictionary<ContentKey, int> _pendingLoadRequests =
    new();

    private readonly ThreadLocal<Stack<ContentLoadFrame>> _loading =
        new(
            static () => new Stack<ContentLoadFrame>(),
            trackAllValues: false);

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

        _source =
            source;

        _catalog =
            catalog;

        _loaders =
            loaders;
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

        return LoadInternal<T>(
            path,
            owner: null);
    }

    public bool IsLoaded<T>(
        AssetPath path)
        where T : class
    {
        EnsureNotDisposed();

        lock (_cacheSync)
        {
            return _cache.TryGetValue(
                       typeof(T),
                       out var typeCache) &&
                   typeCache.ContainsKey(
                       path);
        }
    }

    public ReadOnlyMemory<byte> ReadBytes(
        AssetPath path)
    {
        EnsureNotDisposed();

        return _source.Load(
            path);
    }

    public bool Unload<T>(
        AssetPath path)
        where T : class
    {
        EnsureNotDisposed();

        lock (_cacheSync)
        {
            if (!_cache.TryGetValue(
                    typeof(T),
                    out var typeCache))
            {
                return false;
            }

            if (!typeCache.TryGetValue(
                    path,
                    out var entry))
            {
                return false;
            }

            if (entry.ExternalReferences <= 0)
            {
                return false;
            }

            entry.ExternalReferences--;

            if (HasReferences(entry))
            {
                return true;
            }

            RemoveEntry(
                new ContentKey(
                    typeof(T),
                    path),
                entry);

            return true;
        }
    }

    public void ClearCache()
    {
        EnsureNotDisposed();

        lock (_cacheSync)
        {
            EnsureNotDisposed();
            ClearCacheCore();
        }
    }

    private void ClearCacheCore()
    {
        foreach (var typeCache in _cache.Values)
        {
            foreach (var entry in typeCache.Values)
            {
                if (entry.Asset is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        _cache.Clear();
        _loading.Value!.Clear();
    }

    public void Dispose()
    {
        lock (_cacheSync)
        {
            if (_disposed)
            {
                return;
            }

            // Запрещаем новые записи в кеш до его очистки.
            _disposed = true;

            ClearCacheCore();
        }

        _loaders.Clear();
        _loading.Dispose();
    }

    T IContentLoadContext.Load<T>(
        AssetPath path)
    {
        EnsureNotDisposed();

        var frame =
            GetCurrentLoadingFrame();

        if (frame is null)
        {
            throw new InvalidOperationException(
                "Content dependencies can only be loaded from an active content loader.");
        }

        return LoadInternal<T>(
            path,
            frame.Key);
    }

    private T LoadInternal<T>(
        AssetPath path,
        ContentKey? owner)
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

        var key =
            new ContentKey(
                typeof(T),
                path);

        if (IsLoading(key))
        {
            throw new InvalidOperationException(
                $"Cyclic content dependency detected for '{typeof(T).Name}' at '{path}'.");
        }

        ContentEntry? existing = null;

        lock (_cacheSync)
        {
            if (_cache.TryGetValue(
                    typeof(T),
                    out var typeCache) &&
                typeCache.TryGetValue(
                    path,
                    out var cached))
            {
                existing = cached;

                if (owner.HasValue)
                {
                    RegisterDependency(
                        owner.Value,
                        key,
                        existing);
                }
                else
                {
                    existing.ExternalReferences++;
                }
            }
            else
            {
                AddPendingLoadRequestLocked(key);
            }
        }

        if (existing is not null)
        {
            return (T)existing.Asset;
        }

        try
        {
            if (!_loaders.TryGet<T>(
                    contentAsset,
                    out var loader))
            {
                throw new InvalidOperationException(
                    $"No content loader is registered for asset '{path}' as '{typeof(T).Name}'.");
            }

            var candidate =
                new Lazy<Task<ContentEntry>>(
                    () =>
                        Task.FromResult(
                            LoadFresh<T>(
                                path,
                                key,
                                loader!,
                                owner)),
                    LazyThreadSafetyMode.ExecutionAndPublication);

            var actual =
                _inFlightLoads.GetOrAdd(
                    key,
                    candidate);

            if (ReferenceEquals(actual, candidate))
            {
                try
                {
                    _ = actual.Value;
                }
                catch
                {
                    _inFlightLoads.TryRemove(
                        key,
                        out Lazy<Task<ContentEntry>>? removed);

                    throw;
                }

                _ = actual.Value.ContinueWith(
                    _ => OnInFlightLoadCompleted(
                        key,
                        actual),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            var entry =
                actual.Value
                    .GetAwaiter()
                    .GetResult();

            if (!ReferenceEquals(actual, candidate))
            {
                if (owner.HasValue)
                {
                    RegisterDependency(
                        owner.Value,
                        key,
                        entry);
                }
                else
                {
                    lock (_cacheSync)
                    {
                        ObjectDisposedException.ThrowIf(
                            _disposed,
                            this);

                        entry.ExternalReferences++;
                    }
                }
            }

            return (T)entry.Asset;
        }
        finally
        {
            ReleasePendingLoadRequest(key);
        }
    }

    private void RegisterDependency(
     ContentKey owner,
     ContentKey dependency,
     ContentEntry dependencyEntry)
    {
        ContentLoadFrame? ownerFrame =
            null;

        foreach (var frame in _loading.Value!)
        {
            if (frame.Key == owner)
            {
                ownerFrame =
                    frame;

                break;
            }
        }

        if (ownerFrame is null)
        {
            var asyncLoading =
                _asyncLoading.Value;

            if (asyncLoading is not null)
            {
                foreach (var frame in asyncLoading)
                {
                    if (frame.Key == owner)
                    {
                        ownerFrame =
                            frame;

                        break;
                    }
                }
            }
        }

        if (ownerFrame is null)
        {
            throw new InvalidOperationException(
                "Content dependency owner is not currently loading.");
        }

        if (!ownerFrame.Dependencies.Add(
                dependency))
        {
            return;
        }

        lock (_cacheSync)
        {
            dependencyEntry.DependencyReferences++;
        }
    }

    private void ReleaseDependency(
    ContentKey dependency)
    {
        lock (_cacheSync)
        {
            if (!_cache.TryGetValue(
                    dependency.Type,
                    out var typeCache) ||
                !typeCache.TryGetValue(
                    dependency.Path,
                    out var entry))
            {
                return;
            }

            if (entry.DependencyReferences <= 0)
            {
                return;
            }

            entry.DependencyReferences--;

            if (HasReferences(entry))
            {
                return;
            }

            RemoveEntry(
                dependency,
                entry);
        }
    }

    private void RemoveEntry(
        ContentKey key,
        ContentEntry entry)
    {
        lock (_cacheSync)
        {
            if (_cache.TryGetValue(
                    key.Type,
                    out var typeCache))
            {
                typeCache.Remove(
                    key.Path);

                if (typeCache.Count == 0)
                {
                    _cache.Remove(
                        key.Type);
                }
            }

            foreach (var dependency in
                     entry.Dependencies)
            {
                ReleaseDependency(
                    dependency);
            }

            if (entry.Asset is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private bool IsLoading(
        ContentKey key)
    {
        foreach (var frame in _loading.Value!)
        {
            if (frame.Key == key)
            {
                return true;
            }
        }

        var asyncLoading =
            _asyncLoading.Value;

        if (asyncLoading is null)
        {
            return false;
        }

        foreach (var frame in asyncLoading)
        {
            if (frame.Key == key)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasReferences(
        ContentEntry entry)
    {
        return entry.ExternalReferences > 0 ||
               entry.DependencyReferences > 0;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private readonly record struct ContentKey(
        Type Type,
        AssetPath Path);

    private sealed class ContentLoadFrame
    {
        public ContentLoadFrame(
            ContentKey key)
        {
            Key =
                key;
        }

        public ContentKey Key { get; }

        public HashSet<ContentKey> Dependencies { get; } =
            new();
    }

    public T Reload<T>(
    AssetPath path)
    where T : class
    {
        EnsureNotDisposed();

        var key = new ContentKey(
            typeof(T),
            path);

        ContentEntry? existing;

        lock (_cacheSync)
        {
            if (!_cache.TryGetValue(
                    typeof(T),
                    out var typeCache) ||
                !typeCache.TryGetValue(
                    path,
                    out existing))
            {
                existing = null;
            }
        }

        if (existing is null)
        {
            return Load<T>(path);
        }

        if (IsLoading(key))
        {
            throw new InvalidOperationException(
                $"Content asset '{path}' is already being loaded.");
        }

        if (_inFlightLoads.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Content asset '{path}' is currently being loaded asynchronously.");
        }

        lock (_cacheSync)
        {
            if (existing.ExternalReferences != 1)
            {
                throw new InvalidOperationException(
                    $"Content asset '{path}' cannot be reloaded while " +
                    $"it has {existing.ExternalReferences} external references.");
            }

            if (existing.DependencyReferences != 0)
            {
                throw new InvalidOperationException(
                    $"Content asset '{path}' cannot be reloaded while " +
                    "other content assets depend on it.");
            }
        }

        if (!_catalog.TryGet(path, out var contentAsset) ||
            contentAsset is null)
        {
            throw new FileNotFoundException(
                $"Content asset '{path}' is not registered.");
        }

        if (!_loaders.TryGet<T>(
                contentAsset,
                out var loader))
        {
            throw new InvalidOperationException(
                $"No content loader is registered for asset '{path}' as '{typeof(T).Name}'.");
        }

        // Загружаем замену отдельно, не трогая текущий кеш.
        var replacement = LoadDetached(
            path,
            key,
            loader!);

        try
        {
            lock (_cacheSync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (!_cache.TryGetValue(
                        typeof(T),
                        out var typeCache) ||
                    !typeCache.TryGetValue(
                        path,
                        out var current) ||
                    !ReferenceEquals(current, existing))
                {
                    throw new InvalidOperationException(
                        $"Content asset '{path}' changed while it was being reloaded.");
                }

                // Пока шла загрузка, другие потребители могли
                // получить старый ресурс. В таком случае не заменяем его.
                if (existing.ExternalReferences != 1 ||
                    existing.DependencyReferences != 0)
                {
                    throw new InvalidOperationException(
                        $"Content asset '{path}' acquired additional references while reloading.");
                }

                typeCache[path] = replacement;
            }
        }
        catch
        {
            DisposeDetachedEntry(replacement);
            throw;
        }

        // Новый ресурс уже находится в кеше — теперь можно освободить старый.
        DisposeDetachedEntry(existing);

        return (T)replacement.Asset;
    }

    private ContentEntry LoadDetached<T>(
    AssetPath path,
    ContentKey key,
    IContentLoader<T> loader)
    where T : class
    {
        var frame = new ContentLoadFrame(key);
        T? asset = null;

        _loading.Value!.Push(frame);

        try
        {
            asset = loader.Load(path, this);

            ArgumentNullException.ThrowIfNull(asset);

            // Новый ресурс ещё не добавлен в кеш.
            return new ContentEntry(
                asset,
                externalReferences: 1,
                frame.Dependencies);
        }
        catch
        {
            foreach (var dependency in frame.Dependencies)
            {
                ReleaseDependency(dependency);
            }

            if (asset is IDisposable disposable)
            {
                disposable.Dispose();
            }

            throw;
        }
        finally
        {
            _loading.Value.Pop();
        }
    }

    private void DisposeDetachedEntry(
        ContentEntry entry)
    {
        foreach (var dependency in entry.Dependencies)
        {
            ReleaseDependency(dependency);
        }

        if (entry.Asset is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public async ValueTask<T> LoadAsync<T>(
        AssetPath path,
        CancellationToken cancellationToken = default)
        where T : class
    {
        EnsureNotDisposed();

        cancellationToken.ThrowIfCancellationRequested();

        var owner =
            GetCurrentLoadingFrame()?.Key;

        return await LoadInternalAsync<T>(
            path,
            owner,
            cancellationToken);
    }

    async ValueTask<T> IContentLoadContext.LoadAsync<T>(
        AssetPath path,
        CancellationToken cancellationToken)
    {
        EnsureNotDisposed();

        var frame =
            GetCurrentLoadingFrame();

        if (frame is null)
        {
            throw new InvalidOperationException(
                "Content dependencies can only be loaded from an active content loader.");
        }

        return await LoadInternalAsync<T>(
            path,
            frame.Key,
            cancellationToken);
    }

    public ValueTask<ReadOnlyMemory<byte>> ReadBytesAsync(
    AssetPath path,
    CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        if (_source is IAsyncAssetSource asyncSource)
        {
            return asyncSource.LoadAsync(
                path,
                cancellationToken);
        }

        return new ValueTask<ReadOnlyMemory<byte>>(
            Task.Run(
                () => _source.Load(path),
                cancellationToken));
    }

    private async ValueTask<T> LoadInternalAsync<T>(
    AssetPath path,
    ContentKey? owner,
    CancellationToken cancellationToken)
    where T : class
    {
        EnsureNotDisposed();

        cancellationToken.ThrowIfCancellationRequested();

        if (!_catalog.TryGet(
                path,
                out var contentAsset) ||
            contentAsset is null)
        {
            throw new FileNotFoundException(
                $"Content asset '{path}' is not registered.");
        }

        var key =
            new ContentKey(
                typeof(T),
                path);

        if (IsLoading(key))
        {
            throw new InvalidOperationException(
                $"Cyclic content dependency detected for '{typeof(T).Name}' at '{path}'.");
        }

        ContentEntry? existing = null;

        lock (_cacheSync)
        {
            if (_cache.TryGetValue(
                    typeof(T),
                    out var typeCache) &&
                typeCache.TryGetValue(
                    path,
                    out var cached))
            {
                existing = cached;

                if (owner.HasValue)
                {
                    RegisterDependency(
                        owner.Value,
                        key,
                        existing);
                }
                else
                {
                    existing.ExternalReferences++;
                }
            }
            else
            {
                AddPendingLoadRequestLocked(key);
            }
        }

        if (existing is not null)
        {
            return (T)existing.Asset;
        }

        try
        {
            if (!_loaders.TryGet<T>(
                    contentAsset,
                    out var loader))
            {
                throw new InvalidOperationException(
                    $"No content loader is registered for asset '{path}' as '{typeof(T).Name}'.");
            }

            var candidate =
                new Lazy<Task<ContentEntry>>(
                    () =>
                        LoadFreshAsync(
                            path,
                            key,
                            loader!,
                            owner),
                    LazyThreadSafetyMode.ExecutionAndPublication);

            var actual =
                _inFlightLoads.GetOrAdd(
                    key,
                    candidate);

            if (ReferenceEquals(actual, candidate))
            {
                try
                {
                    _ = actual.Value;
                }
                catch
                {
                    _inFlightLoads.TryRemove(
                        new KeyValuePair<
                            ContentKey,
                            Lazy<Task<ContentEntry>>>(
                            key,
                            actual));

                    throw;
                }

                _ = actual.Value.ContinueWith(
                    _ => OnInFlightLoadCompleted(
                        key,
                        actual),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            var entry =
                await actual.Value.WaitAsync(
                    cancellationToken);

            if (owner.HasValue)
            {
                RegisterDependency(
                    owner.Value,
                    key,
                    entry);
            }
            else
            {
                lock (_cacheSync)
                {
                    ObjectDisposedException.ThrowIf(
                        _disposed,
                        this);

                    entry.ExternalReferences++;
                }
            }

            return (T)entry.Asset;
        }
        finally
        {
            ReleasePendingLoadRequest(key);
        }
    }

    private ContentEntry LoadFresh<T>(
    AssetPath path,
    ContentKey key,
    IContentLoader<T> loader,
    ContentKey? owner)
    where T : class
    {
        var frame =
            new ContentLoadFrame(
                key);

        _loading.Value!.Push(
            frame);
        T? asset = null;
        try
        {

            asset =
                loader.Load(
                    path,
                    this);

            ArgumentNullException.ThrowIfNull(
                asset);

            var entry =
                new ContentEntry(
                    asset,
                    owner.HasValue
                        ? 0
                        : 1,
                    frame.Dependencies);

            lock (_cacheSync)
            {
                if (!_cache.TryGetValue(
                        typeof(T),
                        out var typeCache))
                {
                    typeCache =
                        new Dictionary<
                            AssetPath,
                            ContentEntry>();

                    _cache.Add(
                        typeof(T),
                        typeCache);
                }

                typeCache.Add(
                    path,
                    entry);
            }

            if (owner.HasValue)
            {
                RegisterDependency(
                    owner.Value,
                    key,
                    entry);
            }

            return entry;
        }
        catch
        {
            foreach (var dependency in frame.Dependencies)
            {
                ReleaseDependency(dependency);
            }

            if (asset is IDisposable disposable)
            {
                disposable.Dispose();
            }

            throw;
        }
        finally
        {
            _loading.Value.Pop();
        }
    }
    private async Task<ContentEntry> LoadFreshAsync<T>(
    AssetPath path,
    ContentKey key,
    IContentLoader<T> loader,
    ContentKey? owner)
    where T : class
    {
        var frame =
            new ContentLoadFrame(
                key);

        var loading =
            _asyncLoading.Value;

        if (loading is null)
        {
            loading =
                new Stack<ContentLoadFrame>();

            _asyncLoading.Value =
                loading;
        }

        loading.Push(
            frame);
        T? asset = null;
        try
        {
            asset =
                await loader.LoadAsync(
                    path,
                    this,
                    CancellationToken.None);

            ArgumentNullException.ThrowIfNull(
                asset);

            var entry = new ContentEntry(
                asset,
                externalReferences: 0,
                frame.Dependencies);

            lock (_cacheSync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (!_cache.TryGetValue(
                        typeof(T),
                        out var typeCache))
                {
                    typeCache =
                        new Dictionary<AssetPath, ContentEntry>();

                    _cache.Add(
                        typeof(T),
                        typeCache);
                }

                typeCache.Add(
                    path,
                    entry);
            }
            return entry;
        }
        catch
        {
            foreach (var dependency in frame.Dependencies)
            {
                ReleaseDependency(dependency);
            }

            if (asset is IDisposable disposable)
            {
                disposable.Dispose();
            }

            throw;
        }
        finally
        {
            loading.Pop();

            if (loading.Count == 0)
            {
                _asyncLoading.Value =
                    null;
            }
        }
    }

    private void AddPendingLoadRequestLocked(
    ContentKey key)
    {
        _pendingLoadRequests.TryGetValue(
            key,
            out var count);

        _pendingLoadRequests[key] =
            checked(count + 1);
    }

    private void ReleasePendingLoadRequest(
        ContentKey key)
    {
        lock (_cacheSync)
        {
            if (!_pendingLoadRequests.TryGetValue(
                    key,
                    out var count))
            {
                return;
            }

            if (count > 1)
            {
                _pendingLoadRequests[key] = count - 1;
            }
            else
            {
                _pendingLoadRequests.Remove(key);
            }

            CleanupCompletedInFlightLoadLocked(key);
        }
    }

    private void OnInFlightLoadCompleted(
        ContentKey key,
        Lazy<Task<ContentEntry>> operation)
    {
        lock (_cacheSync)
        {
            CleanupCompletedInFlightLoadLocked(
                key,
                operation);
        }
    }

    private void CleanupCompletedInFlightLoadLocked(
        ContentKey key,
        Lazy<Task<ContentEntry>>? expectedOperation = null)
    {
        if (_pendingLoadRequests.TryGetValue(
                key,
                out var pendingCount) &&
            pendingCount > 0)
        {
            return;
        }

        if (!_inFlightLoads.TryGetValue(
                key,
                out var operation))
        {
            return;
        }

        if (expectedOperation is not null &&
            !ReferenceEquals(operation, expectedOperation))
        {
            return;
        }

        if (!operation.IsValueCreated)
        {
            return;
        }

        var task = operation.Value;

        if (!task.IsCompleted)
        {
            return;
        }

        _inFlightLoads.TryRemove(
            new KeyValuePair<
                ContentKey,
                Lazy<Task<ContentEntry>>>(
                key,
                operation));

        if (!task.IsCompletedSuccessfully)
        {
            return;
        }

        var entry = task.Result;

        if (!_cache.TryGetValue(
                key.Type,
                out var typeCache) ||
            !typeCache.TryGetValue(
                key.Path,
                out var cached) ||
            !ReferenceEquals(cached, entry) ||
            HasReferences(entry))
        {
            return;
        }

        RemoveEntry(
            key,
            entry);
    }

    private ContentLoadFrame? GetCurrentLoadingFrame()
    {
        var loading =
            _loading.Value!;

        if (loading.Count > 0)
        {
            return loading.Peek();
        }

        var asyncLoading =
            _asyncLoading.Value;

        if (asyncLoading is not null &&
            asyncLoading.Count > 0)
        {
            return asyncLoading.Peek();
        }

        return null;
    }

    private sealed class ContentEntry
    {
        public ContentEntry(
            object asset,
            int externalReferences,
            HashSet<ContentKey> dependencies)
        {
            Asset =
                asset;

            ExternalReferences =
                externalReferences;

            Dependencies =
                dependencies;
        }

        public object Asset { get; }

        public int ExternalReferences { get; set; }

        public int DependencyReferences { get; set; }

        public HashSet<ContentKey> Dependencies { get; }
    }
}