using Engine.ECS.Prefabs;
using Engine.Editor.Documents;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Tests.Editor;

public sealed class PrefabDocumentCommandHistoryTests
{
    [Fact]
    public void SetComponentValue_SupportsUndoRedoAndSave()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        var path = Path.Combine(directory, "Test.prefab");

        Directory.CreateDirectory(directory);

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
                        new PrefabComponent[]
                        {
                            new PrefabComponent<TestValueComponent>(
                                new TestValueComponent(42))
                        })
                });

            saveLoad.Save(path, asset);

            var document = PrefabDocument.Open(path, saveLoad);

            Assert.False(document.IsDirty);
            Assert.False(document.CommandHistory.CanUndo);

            document.SetComponentValue(
                entityId,
                typeof(TestValueComponent),
                new TestValueComponent(100));

            Assert.Equal(
                new TestValueComponent(100),
                document.GetComponent(
                    entityId,
                    typeof(TestValueComponent)).BoxedValue);

            Assert.True(document.IsDirty);
            Assert.True(document.CommandHistory.CanUndo);

            document.Undo();

            Assert.Equal(
                new TestValueComponent(42),
                document.GetComponent(
                    entityId,
                    typeof(TestValueComponent)).BoxedValue);

            Assert.False(document.IsDirty);
            Assert.True(document.CommandHistory.CanRedo);

            document.Redo();

            Assert.Equal(
                new TestValueComponent(100),
                document.GetComponent(
                    entityId,
                    typeof(TestValueComponent)).BoxedValue);

            Assert.True(document.IsDirty);

            document.Save();

            Assert.False(document.IsDirty);

            var restored = saveLoad.Load(path);

            Assert.Equal(
                new TestValueComponent(100),
                restored.Entities[0]
                    .Components[0]
                    .BoxedValue);
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
            return new TestValueComponent(
                reader.ReadInt32());
        }
    }
}