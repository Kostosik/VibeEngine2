using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;

namespace Engine.Tests.Content;

public sealed class ContentManagerTests
{
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