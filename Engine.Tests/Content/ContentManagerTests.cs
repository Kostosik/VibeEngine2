using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;

namespace Engine.Tests.Content;

public sealed class ContentManagerTests
{
    [Fact]
    public async Task LoadAsync_CanLoadDependentAssetThroughContext()
    {
        var dependencyPath =
            new AssetPath(
                "dependency.asset");

        var wrapperPath =
            new AssetPath(
                "wrapper.asset");

        var content =
            new ContentManager(
                new TestAssetSource(),
                new TestCatalog(
                    dependencyPath,
                    wrapperPath),
                new ContentLoaderRegistry());

        content.Register(
            new TestLoader());

        content.Register(
            new AsyncWrapperLoader(
                dependencyPath));

        var wrapper =
            await content.LoadAsync<AsyncWrapperAsset>(
                wrapperPath);

        Assert.NotNull(
            wrapper.Value);
    }

    [Fact]
    public void Load_ThrowsForCyclicDependency()
    {
        var pathA =
            new AssetPath("a.asset");

        var pathB =
            new AssetPath("b.asset");

        var content =
            new ContentManager(
                new TestAssetSource(),
                new TestCatalog(
                    pathA,
                    pathB),
                new ContentLoaderRegistry());

        content.Register(
            new CyclicLoaderA(
                pathB));

        content.Register(
            new CyclicLoaderB(
                pathA));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                content.Load<TestAssetA>(
                    pathA);
            });
    }

    [Fact]
    public void Load_CachesLoadedAsset()
    {
        var source =
            new TestAssetSource();

        var catalog =
            new TestCatalog(
                new AssetPath("test.asset"));

        var loaders =
            new ContentLoaderRegistry();

        using var content =
            new ContentManager(
                source,
                catalog,
                loaders);

        var loader =
            new TestLoader();

        content.Register(
            loader);

        var first =
            content.Load<TestAsset>(
                new AssetPath("test.asset"));

        var second =
            content.Load<TestAsset>(
                new AssetPath("test.asset"));

        Assert.Same(
            first,
            second);

        Assert.Equal(
            1,
            loader.LoadCount);
    }

    [Fact]
    public async Task LoadAsync_ConcurrentRequestsShareInFlightLoad()
    {
        var path =
            new AssetPath(
                "test.asset");

        var catalog =
            new TestCatalog(
                path);

        var loaders =
            new ContentLoaderRegistry();

        using var content =
            new ContentManager(
                new TestAssetSource(),
                catalog,
                loaders);

        var loader =
            new BlockingAsyncLoader();

        content.Register(
            loader);

        var firstTask =
            content.LoadAsync<TestAsset>(
                path)
            .AsTask();

        await loader.Started.Task;

        var secondTask =
            content.LoadAsync<TestAsset>(
                path)
            .AsTask();

        loader.Release();

        var results =
            await Task.WhenAll(
                firstTask,
                secondTask);

        Assert.Same(
            results[0],
            results[1]);

        Assert.Equal(
            1,
            loader.LoadCount);

        Assert.True(
            content.Unload<TestAsset>(
                path));

        Assert.True(
            content.Unload<TestAsset>(
                path));
    }

    private sealed class BlockingAsyncLoader :
    IContentLoader<TestAsset>
    {
        private readonly
            TaskCompletionSource<bool> _release =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        private int _loadCount;

        public TaskCompletionSource<bool> Started { get; } =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public int LoadCount =>
            _loadCount;

        public Type AssetType =>
            typeof(TestAsset);

        public TestAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            Interlocked.Increment(
                ref _loadCount);

            return new TestAsset();
        }

        public async ValueTask<TestAsset> LoadAsync(
            AssetPath path,
            IContentLoadContext context,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(
                ref _loadCount);

            Started.TrySetResult(
                true);

            await _release.Task.WaitAsync(
                cancellationToken);

            return new TestAsset();
        }

        public void Release()
        {
            _release.TrySetResult(
                true);
        }

        object IContentLoader.Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return Load(
                path,
                context);
        }

        public bool CanLoad(
            ContentAsset asset)
        {
            ArgumentNullException.ThrowIfNull(
                asset);

            return true;
        }
    }

    [Fact]
    public void Reload_ReplacesSoleExternalReference()
    {
        var path =
            new AssetPath(
                "test.asset");

        var loaders =
            new ContentLoaderRegistry();

        using var content =
            new ContentManager(
                new TestAssetSource(),
                new TestCatalog(
                    path),
                loaders);

        var loader =
            new TestLoader();

        content.Register(
            loader);

        var first =
            content.Load<TestAsset>(
                path);

        var second =
            content.Reload<TestAsset>(
                path);

        Assert.NotSame(
            first,
            second);

        Assert.Equal(
            2,
            loader.LoadCount);

        Assert.True(
            content.IsLoaded<TestAsset>(
                path));

        Assert.True(
            content.Unload<TestAsset>(
                path));
    }

    [Fact]
    public void Reload_RejectsMultipleExternalReferences()
    {
        var path =
            new AssetPath(
                "test.asset");

        var loaders =
            new ContentLoaderRegistry();

        using var content =
            new ContentManager(
                new TestAssetSource(),
                new TestCatalog(
                    path),
                loaders);

        var loader =
            new TestLoader();

        content.Register(
            loader);

        var first =
            content.Load<TestAsset>(
                path);

        var second =
            content.Load<TestAsset>(
                path);

        Assert.Same(
            first,
            second);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    content.Reload<TestAsset>(
                        path);
                });

        Assert.Contains(
            "external references",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            1,
            loader.LoadCount);

        Assert.Same(
            first,
            content.Load<TestAsset>(
                path));

        Assert.True(
            content.Unload<TestAsset>(
                path));

        Assert.True(
            content.Unload<TestAsset>(
                path));

        _ = second;
    }

    [Fact]
    public void Reload_RejectsAssetUsedAsDependency()
    {
        var dependencyPath =
            new AssetPath(
                "dependency.asset");

        var wrapperPath =
            new AssetPath(
                "wrapper.asset");

        var loaders =
            new ContentLoaderRegistry();

        using var content =
            new ContentManager(
                new TestAssetSource(),
                new TestCatalog(
                    dependencyPath,
                    wrapperPath),
                loaders);

        content.Register(
            new TestLoader());

        content.Register(
            new WrapperLoader(
                dependencyPath));

        _ = content.Load<WrapperAsset>(
            wrapperPath);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    content.Reload<TestAsset>(
                        dependencyPath);
                });

        Assert.Contains(
            "depend",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class WrapperAsset
    {
        public WrapperAsset(
            TestAsset value)
        {
            Value =
                value;
        }

        public TestAsset Value
        {
            get;
        }
    }

    private sealed class WrapperLoader :
        IContentLoader<WrapperAsset>
    {
        private readonly AssetPath _dependencyPath;

        public WrapperLoader(
            AssetPath dependencyPath)
        {
            _dependencyPath =
                dependencyPath;
        }

        public Type AssetType =>
            typeof(WrapperAsset);

        public WrapperAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return new WrapperAsset(
                context.Load<TestAsset>(
                    _dependencyPath));
        }

        object IContentLoader.Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return Load(
                path,
                context);
        }

        public bool CanLoad(
            ContentAsset asset)
        {
            ArgumentNullException.ThrowIfNull(
                asset);

            return true;
        }
    }

    [Fact]
    public void Unload_DisposesLoadedAsset()
    {
        var source =
            new TestAssetSource();

        var path =
            new AssetPath("test.asset");

        var catalog =
            new TestCatalog(path);

        var loaders =
            new ContentLoaderRegistry();

        using var content =
            new ContentManager(
                source,
                catalog,
                loaders);

        content.Register(
            new DisposableTestLoader());

        var asset =
            content.Load<DisposableTestAsset>(
                path);

        Assert.False(
            asset.Disposed);

        Assert.True(
            content.Unload<DisposableTestAsset>(
                path));

        Assert.True(
            asset.Disposed);

        Assert.False(
            content.IsLoaded<DisposableTestAsset>(
                path));
    }

    private sealed class AsyncWrapperAsset
    {
        public AsyncWrapperAsset(
            TestAsset value)
        {
            Value =
                value;
        }

        public TestAsset Value
        {
            get;
        }
    }

    private sealed class AsyncWrapperLoader :
        IContentLoader<AsyncWrapperAsset>
    {
        private readonly AssetPath _dependencyPath;

        public AsyncWrapperLoader(
            AssetPath dependencyPath)
        {
            _dependencyPath =
                dependencyPath;
        }

        public Type AssetType =>
            typeof(AsyncWrapperAsset);

        public AsyncWrapperAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return new AsyncWrapperAsset(
                context.Load<TestAsset>(
                    _dependencyPath));
        }

        public async ValueTask<AsyncWrapperAsset> LoadAsync(
            AssetPath path,
            IContentLoadContext context,
            CancellationToken cancellationToken = default)
        {
            var dependency =
                await context.LoadAsync<TestAsset>(
                    _dependencyPath,
                    cancellationToken);

            return new AsyncWrapperAsset(
                dependency);
        }

        object IContentLoader.Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return Load(
                path,
                context);
        }

        public bool CanLoad(
            ContentAsset asset)
        {
            ArgumentNullException.ThrowIfNull(
                asset);

            return true;
        }
    }
    private sealed class DisposableTestAsset :
        IDisposable
    {
        public bool Disposed
        {
            get;
            private set;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class DisposableTestLoader :
        IContentLoader<DisposableTestAsset>
    {
        public Type AssetType =>
            typeof(DisposableTestAsset);

        public DisposableTestAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return new DisposableTestAsset();
        }

        object IContentLoader.Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return Load(
                path,
                context);
        }

        public bool CanLoad(
            ContentAsset asset)
        {
            ArgumentNullException.ThrowIfNull(
                asset);

            return true;
        }
    }

    private sealed class TestAsset
    {
    }

    private sealed class TestLoader :
        IContentLoader<TestAsset>
    {
        public int LoadCount
        {
            get;
            private set;
        }

        public Type AssetType =>
            typeof(TestAsset);

        public TestAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            LoadCount++;

            return new TestAsset();
        }

        object IContentLoader.Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return Load(
                path,
                context);
        }

        public bool CanLoad(
            ContentAsset asset)
        {
            ArgumentNullException.ThrowIfNull(
                asset);

            return true;
        }
    }

    private sealed class TestAssetSource :
        IAssetSource
    {
        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
    }

    private sealed class TestCatalog :
        IContentCatalog
    {
        private readonly Dictionary<
            AssetPath,
            ContentAsset> _assets =
            new();

        public TestCatalog(
            params AssetPath[] paths)
        {
            foreach (var path in paths)
            {
                _assets.Add(
                    path,
                    new ContentAsset(
                        path,
                        ".asset",
                        0,
                        DateTime.UnixEpoch));
            }
        }

        public IReadOnlyList<ContentAsset> GetAssets()
        {
            return _assets.Values.ToArray();
        }

        public bool Contains(
            AssetPath path)
        {
            return _assets.ContainsKey(
                path);
        }

        public bool TryGet(
            AssetPath path,
            out ContentAsset? asset)
        {
            return _assets.TryGetValue(
                path,
                out asset);
        }

        public void Refresh()
        {
        }
    }

    private sealed class TestAssetA
    {
        public TestAssetA(
            TestAssetB dependency)
        {
            Dependency =
                dependency;
        }

        public TestAssetB Dependency
        {
            get;
        }
    }

    private sealed class TestAssetB
    {
        public TestAssetB(
            TestAssetA dependency)
        {
            Dependency =
                dependency;
        }

        public TestAssetA Dependency
        {
            get;
        }
    }

    private sealed class CyclicLoaderA :
        IContentLoader<TestAssetA>
    {
        private readonly AssetPath _dependencyPath;

        public CyclicLoaderA(
            AssetPath dependencyPath)
        {
            _dependencyPath =
                dependencyPath;
        }

        public Type AssetType =>
            typeof(TestAssetA);

        public TestAssetA Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return new TestAssetA(
                context.Load<TestAssetB>(
                    _dependencyPath));
        }

        object IContentLoader.Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return Load(
                path,
                context);
        }

        public bool CanLoad(
            ContentAsset asset)
        {
            ArgumentNullException.ThrowIfNull(
                asset);

            return true;
        }
    }

    private sealed class CyclicLoaderB :
        IContentLoader<TestAssetB>
    {
        private readonly AssetPath _dependencyPath;

        public CyclicLoaderB(
            AssetPath dependencyPath)
        {
            _dependencyPath =
                dependencyPath;
        }

        public Type AssetType =>
            typeof(TestAssetB);

        public TestAssetB Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return new TestAssetB(
                context.Load<TestAssetA>(
                    _dependencyPath));
        }

        object IContentLoader.Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return Load(
                path,
                context);
        }

        public bool CanLoad(
            ContentAsset asset)
        {
            ArgumentNullException.ThrowIfNull(
                asset);

            return true;
        }
    }
}