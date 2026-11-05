using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.ECS.Prefabs;
using Engine.Serialization.Binary;
using Engine.Serialization.Content;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Tests.Content;

public sealed class PrefabAssetContentLoaderTests
{
    [Fact]
    public void Load_LoadsPrefabThroughContentManager()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        var prefabDirectory = Path.Combine(root, "Prefabs");

        Directory.CreateDirectory(prefabDirectory);

        try
        {
            var registry = CreateRegistry();
            var asset = CreatePrefab();

            var serializer = new PrefabAssetSerializer(registry);

            var data = BinarySerializer.SerializeContainer(
                asset,
                serializer,
                SerializationContext.Default);

            File.WriteAllBytes(
                Path.Combine(prefabDirectory, "Crate.prefab"),
                data);

            using var content = CreateContent(root, registry);

            var loaded = content.Load<PrefabAsset>(
                new AssetPath("Prefabs/Crate.prefab"));

            Assert.Equal(asset.Name, loaded.Name);
            Assert.Equal(asset.RootEntity, loaded.RootEntity);
            Assert.Single(loaded.Entities);

            var component = Assert.Single(
                loaded.Entities[0].Components.OfType<
                    PrefabComponent<TestComponent>>());

            Assert.Equal(123, component.Component.Value);
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
    public async Task LoadAsync_LoadsPrefabThroughContentManager()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);

        try
        {
            var registry = CreateRegistry();
            var asset = CreatePrefab();

            var data = BinarySerializer.SerializeContainer(
                asset,
                new PrefabAssetSerializer(registry),
                SerializationContext.Default);

            File.WriteAllBytes(
                Path.Combine(root, "Async.prefab"),
                data);

            using var content = CreateContent(root, registry);

            var loaded = await content.LoadAsync<PrefabAsset>(
                new AssetPath("Async.prefab"));

            Assert.Equal(asset.Name, loaded.Name);
            Assert.Equal(asset.RootEntity, loaded.RootEntity);
            Assert.Single(loaded.Entities);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ContentManager CreateContent(
        string root,
        EcsComponentSerializerRegistry registry)
    {
        var content = new ContentManager(
            new FileAssetSource(root),
            new FileContentCatalog(root),
            new ContentLoaderRegistry());

        content.Register(
            new PrefabAssetContentLoader(registry));

        return content;
    }

    private static EcsComponentSerializerRegistry CreateRegistry()
    {
        var registry = new EcsComponentSerializerRegistry();

        registry.Register(
            "test.prefab.component",
            new TestComponentSerializer());

        return registry;
    }

    private static PrefabAsset CreatePrefab()
    {
        var root = new PrefabEntityId(1);

        return new PrefabAsset(
            "TestCrate",
            root,
            new[]
            {
                new PrefabEntity(
                    root,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<TestComponent>(
                            new TestComponent(123))
                    })
            });
    }

    private readonly record struct TestComponent(int Value);

    private sealed class TestComponentSerializer :
        IBinarySerializer<TestComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            TestComponent value)
        {
            writer.WriteInt32(value.Value);
        }

        public TestComponent Deserialize(
            ref SerializationReader reader)
        {
            return new TestComponent(
                reader.ReadInt32());
        }
    }
}