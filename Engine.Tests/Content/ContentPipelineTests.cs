using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.HotReload;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Serialization.Binary;
using Engine.Serialization.Content;

namespace Engine.Tests.Content;

public sealed class ContentPipelineTests
{
    [Fact]
    public void HotReload_Update_ReportsAssetThatReappearsAfterRemoval()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(root);

        try
        {
            var filePath = Path.Combine(root, "test.asset");
            var path = new AssetPath("test.asset");

            File.WriteAllBytes(filePath, new byte[] { 1, 2, 3 });

            var source = new FileAssetSource(root);
            var catalog = new FileContentCatalog(root);
            var loaders = new ContentLoaderRegistry();

            using var content = new ContentManager(source, catalog, loaders);
            content.Register(new TestAssetLoader());

            var original = content.Load<TestAsset>(path);
            var hotReload = new ContentHotReloadService(catalog, content);

            File.Delete(filePath);

            Assert.Empty(hotReload.Update());

            File.WriteAllBytes(filePath, new byte[] { 4, 5, 6 });

            var changed = hotReload.Update();

            Assert.Contains(path, changed);

            Assert.True(hotReload.Reload<TestAsset>(path));

            var reloaded = content.Load<TestAsset>(path);

            Assert.NotSame(original, reloaded);
            Assert.Equal(new byte[] { 4, 5, 6 }, reloaded.Data);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Refresh_WhenRootDisappears_PreservesPreviousSnapshot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, "test.asset"),
                "test");

            var catalog = new FileContentCatalog(root);

            Assert.Single(catalog.GetAssets());

            Directory.Delete(root, recursive: true);

            Assert.Throws<DirectoryNotFoundException>(
                () => catalog.Refresh());

            // Неудачное обновление не должно уничтожать
            // последний успешно полученный снимок.
            Assert.Single(catalog.GetAssets());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Load_ReadsAssetThroughCatalogSourceAndLoader()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            root);

        try
        {
            var filePath =
                Path.Combine(
                    root,
                    "test.asset");

            File.WriteAllBytes(
                filePath,
                new byte[]
                {
                    1,
                    2,
                    3
                });

            var source =
                new FileAssetSource(
                    root);

            var catalog =
                new FileContentCatalog(
                    root);

            var loaders =
                new ContentLoaderRegistry();

            using var content =
                new ContentManager(
                    source,
                    catalog,
                    loaders);

            content.Register(
                new TestAssetLoader());

            var asset =
                content.Load<TestAsset>(
                    new AssetPath(
                        "test.asset"));

            Assert.Equal(
                new byte[]
                {
                    1,
                    2,
                    3
                },
                asset.Data);
        }
        finally
        {
            Directory.Delete(
                root,
                true);
        }
    }

    [Fact]
    public void BinaryContentLoader_LoadsSerializedAssetThroughContent()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            root);

        try
        {
            var path =
                Path.Combine(
                    root,
                    "test.bin");

            var expected =
                new BinaryTestAsset(
                    "hello");

            var serializer =
                new TestAssetSerializer();

            File.WriteAllBytes(
                path,
                BinarySerializer.SerializeContainer(
                    expected,
                    serializer,
                    SerializationContext.Default));

            var loaders =
                new ContentLoaderRegistry();

            using var content =
                new ContentManager(
                    new FileAssetSource(
                        root),
                    new FileContentCatalog(
                        root),
                    loaders);

            content.Register(
                new BinaryContentLoader<BinaryTestAsset>(
                    serializer,
                    asset => asset.Extension.Equals(
                        ".bin",
                        StringComparison.OrdinalIgnoreCase)));

            var actual =
                content.Load<BinaryTestAsset>(
                    new AssetPath(
                        "test.bin"));

            Assert.Equal(
                expected.Value,
                actual.Value);
        }
        finally
        {
            Directory.Delete(
                root,
                true);
        }
    }

    [Fact]
    public void LoaderCanLoadDependentAssetThroughContext()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            root);

        try
        {
            File.WriteAllText(
                Path.Combine(
                    root,
                    "value.txt"),
                "hello");

            File.WriteAllText(
                Path.Combine(
                    root,
                    "wrapper.asset"),
                "value.txt");

            var source =
                new FileAssetSource(
                    root);

            var catalog =
                new FileContentCatalog(
                    root);

            var loaders =
                new ContentLoaderRegistry();

            using var content =
                new ContentManager(
                    source,
                    catalog,
                    loaders);

            content.Register(
                new TextAssetLoader());

            content.Register(
                new WrapperAssetLoader());

            var wrapper =
                content.Load<WrapperAsset>(
                    new AssetPath(
                        "wrapper.asset"));

            Assert.Equal(
                "hello",
                wrapper.Value.Text);
        }
        finally
        {
            Directory.Delete(
                root,
                true);
        }
    }

    private sealed class TestAssetSerializer :
        IBinarySerializer<BinaryTestAsset>
    {
        public void Serialize(
            ref SerializationWriter writer,
            BinaryTestAsset value)
        {
            writer.WriteString(
                value.Value);
        }

        public BinaryTestAsset Deserialize(
            ref SerializationReader reader)
        {
            return new BinaryTestAsset(
                reader.ReadString()
                ?? throw new InvalidDataException(
                    "Binary test asset value cannot be null."));
        }
    }

    private sealed class BinaryTestAsset
    {
        public BinaryTestAsset(
            string value)
        {
            Value =
                value;
        }

        public string Value
        {
            get;
        }
    }

    private sealed class TextAsset
    {
        public TextAsset(
            string text)
        {
            Text =
                text;
        }

        public string Text
        {
            get;
        }
    }

    private sealed class TextAssetLoader :
        IContentLoader<TextAsset>
    {
        public Type AssetType =>
            typeof(TextAsset);

        public TextAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return new TextAsset(
                System.Text.Encoding.UTF8.GetString(
                    context.ReadBytes(
                        path)
                    .Span));
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

    private sealed class WrapperAsset
    {
        public WrapperAsset(
            TextAsset value)
        {
            Value =
                value;
        }

        public TextAsset Value
        {
            get;
        }
    }

    private sealed class WrapperAssetLoader :
        IContentLoader<WrapperAsset>
    {
        public Type AssetType =>
            typeof(WrapperAsset);

        public WrapperAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            var dependencyPath =
                System.Text.Encoding.UTF8.GetString(
                    context.ReadBytes(
                        path)
                    .Span);

            return new WrapperAsset(
                context.Load<TextAsset>(
                    new AssetPath(
                        dependencyPath)));
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
        public TestAsset(
            byte[] data)
        {
            Data =
                data;
        }

        public byte[] Data
        {
            get;
        }
    }

    private sealed class TestAssetLoader :
        IContentLoader<TestAsset>
    {
        public Type AssetType =>
            typeof(TestAsset);

        public TestAsset Load(
            AssetPath path,
            IContentLoadContext context)
        {
            return new TestAsset(
                context
                    .ReadBytes(
                        path)
                    .ToArray());
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