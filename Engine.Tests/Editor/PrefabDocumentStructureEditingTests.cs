using Engine.ECS.Prefabs;
using Engine.Editor.Documents;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Tests.Editor;

public sealed class PrefabDocumentStructureEditingTests
{
    [Fact]
    public void AddRemoveComponent_SupportsUndoRedo()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "Test.prefab");

        try
        {
            var registry = new EcsComponentSerializerRegistry();

            registry.Register(
                "test.value",
                new TestValueComponentSerializer());

            var saveLoad = new PrefabAssetSaveLoadService(registry);
            var entityId = new PrefabEntityId(1);

            var asset = new PrefabAsset(
                "Test",
                entityId,
                new[]
                {
                    new PrefabEntity(
                        entityId,
                        Array.Empty<PrefabComponent>())
                });

            saveLoad.Save(path, asset);

            var document = PrefabDocument.Open(path, saveLoad);

            Assert.Empty(document.Asset.Entities.Single().Components);
            Assert.False(document.IsDirty);

            document.AddComponent(
                entityId,
                PrefabComponent.Create(
                    typeof(TestValueComponent),
                    new TestValueComponent(42)));

            Assert.True(document.IsDirty);
            Assert.Equal(
                new TestValueComponent(42),
                document.GetComponent(
                    entityId,
                    typeof(TestValueComponent)).BoxedValue);

            document.Undo();

            Assert.Empty(document.Asset.Entities.Single().Components);
            Assert.False(document.IsDirty);

            document.Redo();

            Assert.Equal(
                new TestValueComponent(42),
                document.GetComponent(
                    entityId,
                    typeof(TestValueComponent)).BoxedValue);

            document.RemoveComponent(
                entityId,
                typeof(TestValueComponent));

            Assert.Empty(document.Asset.Entities.Single().Components);

            document.Undo();

            Assert.Equal(
                new TestValueComponent(42),
                document.GetComponent(
                    entityId,
                    typeof(TestValueComponent)).BoxedValue);

            document.Redo();

            Assert.Empty(document.Asset.Entities.Single().Components);
            Assert.True(document.IsDirty);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private readonly record struct TestValueComponent(int Value);

    private sealed class TestValueComponentSerializer
        : IBinarySerializer<TestValueComponent>
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
            return new TestValueComponent(reader.ReadInt32());
        }
    }
}