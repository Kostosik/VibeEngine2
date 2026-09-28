using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;

namespace Engine.Tests.Content;

public sealed class ContentManagerTests
{
    [Fact]
    public void Load_CachesLoadedAsset()
    {
        var source =
            new TestAssetSource();

        var catalog =
            new TestCatalog(
                new AssetPath("test.asset"));

        using var content =
            new ContentManager(
                source,
                catalog);

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

        public TestAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            LoadCount++;

            return new TestAsset();
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
        private readonly ContentAsset _asset;

        public TestCatalog(
            AssetPath path)
        {
            _asset =
                new ContentAsset(
                    path,
                    ".asset",
                    0);
        }

        public IReadOnlyList<ContentAsset> GetAssets()
        {
            return new[] { _asset };
        }

        public bool Contains(
            AssetPath path)
        {
            return path == _asset.Path;
        }

        public bool TryGet(
            AssetPath path,
            out ContentAsset? asset)
        {
            if (path == _asset.Path)
            {
                asset = _asset;
                return true;
            }

            asset = null;
            return false;
        }
    }
}