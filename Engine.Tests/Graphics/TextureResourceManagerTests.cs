using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics;

public sealed class TextureResourceManagerTests
{
    [Fact]
    public void Load_SameAssetTwice_ReturnsSameTexture()
    {
        var content =
            CreateContent();

        var textures =
            new FakeTextureManager();

        using var resources =
            new TextureResourceManager(
                content,
                textures);

        var path =
            new AssetPath(
                "Textures/test.png");

        var first =
            resources.Load(path);

        var second =
            resources.Load(path);

        Assert.Equal(
            first,
            second);

        Assert.Equal(
            1,
            textures.CreateCount);
    }

    [Fact]
    public void LoadAtlas_SameParameters_ReturnsSameAtlas()
    {
        var content =
            CreateContent();

        var textures =
            new FakeTextureManager();

        using var resources =
            new TextureResourceManager(
                content,
                textures);

        var path =
            new AssetPath(
                "Textures/test.png");

        var first =
            resources.LoadAtlas(
                path,
                1,
                1);

        var second =
            resources.LoadAtlas(
                path,
                1,
                1);

        Assert.Same(
            first,
            second);

        Assert.Equal(
            1,
            textures.CreateCount);
    }

    private static IContentManager CreateContent()
    {
        var assets =
            new MemoryAssetSource(
                new Dictionary<string, byte[]>
                {
                    ["Textures/test.png"] =
                        CreatePng()
                });

        var catalog =
            new MemoryContentCatalog(
                new AssetPath(
                    "Textures/test.png"));

        var loaders =
            new ContentLoaderRegistry();

        loaders.Register(
            new ImageTextureContentLoader());

        return new ContentManager(
            assets,
            catalog,
            loaders);
    }

    private static byte[] CreatePng()
    {
        return
        [
            137, 80, 78, 71, 13, 10, 26, 10,
            0, 0, 0, 13, 73, 72, 68, 82,
            0, 0, 0, 1,
            0, 0, 0, 1,
            8, 6, 0, 0, 0,
            31, 21, 196, 137,
            0, 0, 0, 13,
            73, 68, 65, 84,
            8, 215, 99, 248, 207, 192, 240, 31, 0, 5, 0, 1, 255,
            137, 153, 61, 29,
            0, 0, 0, 0,
            73, 69, 78, 68,
            174, 66, 96, 130
        ];
    }

    private sealed class MemoryAssetSource :
        IAssetSource
    {
        private readonly IReadOnlyDictionary<string, byte[]> _assets;

        public MemoryAssetSource(
            IReadOnlyDictionary<string, byte[]> assets)
        {
            _assets =
                assets;
        }

        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            return _assets[path.Value];
        }
    }

    private sealed class MemoryContentCatalog :
        IContentCatalog
    {
        private readonly Dictionary<
            AssetPath,
            ContentAsset> _assets =
            new();

        public MemoryContentCatalog(
            params AssetPath[] paths)
        {
            foreach (var path in paths)
            {
                _assets.Add(
                    path,
                    new ContentAsset(
                        path,
                        Path.GetExtension(
                            path.Value),
                        0,
                        DateTime.UnixEpoch));
            }
        }

        public void Refresh()
        { }

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
    }

    private sealed class FakeTextureManager :
        ITextureManager
    {
        private readonly Dictionary<
            TextureHandle,
            TextureDescription> _textures =
            new();

        private uint _nextId = 1;

        public int CreateCount
        {
            get;
            private set;
        }

        public TextureHandle Create(
            TextureDescription description)
        {
            var handle =
                new TextureHandle(
                    _nextId++);

            _textures.Add(
                handle,
                description);

            CreateCount++;

            return handle;
        }

        public TextureHandle Create(
            TextureData data)
        {
            return Create(
                data.Description);
        }

        public bool Exists(
            TextureHandle texture)
        {
            return _textures.ContainsKey(
                texture);
        }

        public TextureDescription GetDescription(
            TextureHandle texture)
        {
            return _textures[texture];
        }

        public void Destroy(
            TextureHandle texture)
        {
            _textures.Remove(
                texture);
        }
    }
}