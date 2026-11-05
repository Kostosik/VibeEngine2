using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;
using Engine.Serialization.Types;

namespace Engine.Tests.Serialization.SaveLoad;

public sealed class PrefabAssetSerializerTests
{
    [Fact]
    public void SerializeDeserialize_PreservesHierarchyAndReferences()
    {
        var asset = CreatePrefab();

        var serializer = new PrefabAssetSerializer(
            CreateRegistry());

        var data = BinarySerializer.Serialize(
            asset,
            serializer);

        var restored = BinarySerializer.Deserialize(
            data,
            serializer);

        Assert.Equal(asset.Name, restored.Name);
        Assert.Equal(asset.RootEntity, restored.RootEntity);
        Assert.Equal(2, restored.Entities.Count);

        var root = Assert.Single(
            restored.Entities.Where(
                entity => entity.Id == new PrefabEntityId(1)));

        var child = Assert.Single(
            restored.Entities.Where(
                entity => entity.Id == new PrefabEntityId(2)));

        var value = Assert.Single(
            root.Components.OfType<
                PrefabComponent<TestValueComponent>>());

        Assert.Equal(42, value.Component.Value);

        var parent = Assert.Single(
            child.Components.OfType<
                PrefabComponent<TransformParent2D>>());

        Assert.Equal(
            new EntityId(1, 0),
            parent.Component.Parent);

        var link = Assert.Single(
            child.Components.OfType<
                PrefabComponent<TestEntityLinkComponent>>());

        Assert.Equal(
            new EntityId(1, 0),
            link.Component.Target);
    }

    [Fact]
    public void SaveLoad_RoundTripsPrefab()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        var path = Path.Combine(
            directory,
            "Test.prefab");

        try
        {
            var service = new PrefabAssetSaveLoadService(
                CreateRegistry());

            var asset = CreatePrefab();

            service.Save(path, asset);

            Assert.True(File.Exists(path));

            var restored = service.Load(path);

            Assert.Equal(asset.Name, restored.Name);
            Assert.Equal(asset.RootEntity, restored.RootEntity);
            Assert.Equal(
                asset.Entities.Count,
                restored.Entities.Count);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void Deserialize_UnsupportedVersion_Throws()
    {
        var writer = new SerializationWriter();

        writer.WriteInt32(2);

        var data = writer.ToArray();

        var serializer = new PrefabAssetSerializer(
            CreateRegistry());

        Assert.Throws<InvalidDataException>(
            () => BinarySerializer.Deserialize(
                data,
                serializer));
    }

    private static PrefabAsset CreatePrefab()
    {
        var root = new PrefabEntityId(1);
        var child = new PrefabEntityId(2);

        return new PrefabAsset(
            "TestPrefab",
            root,
            new[]
            {
                new PrefabEntity(
                    root,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<TestValueComponent>(
                            new TestValueComponent(42))
                    }),

                new PrefabEntity(
                    child,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<TransformParent2D>(
                            new TransformParent2D(
                                new EntityId(1, 0))),

                        new PrefabComponent<TestEntityLinkComponent>(
                            new TestEntityLinkComponent(
                                new EntityId(1, 0)))
                    })
            });
    }

    private static EcsComponentSerializerRegistry CreateRegistry()
    {
        var registry = new EcsComponentSerializerRegistry();

        registry.Register(
            "test.value",
            new TestValueComponentSerializer());

        registry.Register(
            "engine.transform.parent2d",
            new TransformParent2DSerializer());

        registry.Register(
            "test.entity-link",
            new TestEntityLinkComponentSerializer());

        return registry;
    }

    private readonly record struct TestValueComponent(int Value);

    private readonly record struct TestEntityLinkComponent(
        EntityId Target) : IPrefabEntityReferenceRemappable
    {
        public object RemapPrefabEntityReferences(
            Func<EntityId, EntityId> remap)
        {
            ArgumentNullException.ThrowIfNull(remap);

            return new TestEntityLinkComponent(
                remap(Target));
        }
    }

    private sealed class TestValueComponentSerializer :
        IBinarySerializer<TestValueComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            TestValueComponent value)
        {
            writer.WriteInt32(value.Value);
        }

        public TestValueComponent Deserialize(
            ref SerializationReader reader)
        {
            return new TestValueComponent(
                reader.ReadInt32());
        }
    }

    private sealed class TestEntityLinkComponentSerializer :
        IBinarySerializer<TestEntityLinkComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            TestEntityLinkComponent value)
        {
            writer.WriteUInt32(value.Target.Index);
            writer.WriteUInt32(value.Target.Generation);
        }

        public TestEntityLinkComponent Deserialize(
            ref SerializationReader reader)
        {
            return new TestEntityLinkComponent(
                new EntityId(
                    reader.ReadUInt32(),
                    reader.ReadUInt32()));
        }
    }
}