using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Editor.Documents;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Tests.Editor;

public sealed class PrefabDocumentEntityEditingTests
{
    [Fact]
    public void CreateAndDeleteChildEntities_SupportsUndoRedoAndSave()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "Test.prefab");

        try
        {
            var registry = new EcsComponentSerializerRegistry();
            registry.RegisterEngineComponents();

            var saveLoad = new PrefabAssetSaveLoadService(registry);

            var rootId = new PrefabEntityId(1);

            var asset = new PrefabAsset(
                "Test",
                rootId,
                new[]
                {
                    new PrefabEntity(
                        rootId,
                        new[]
                        {
                            PrefabComponent.Create(
                                typeof(WorldTransform2D),
                                new WorldTransform2D())
                        })
                });

            saveLoad.Save(path, asset);

            var document = PrefabDocument.Open(path, saveLoad);

            var childId = document.CreateChildEntity(rootId);
            var grandchildId = document.CreateChildEntity(childId);

            Assert.Equal(3, document.Asset.Entities.Count);

            var childParent = (TransformParent2D)
                document.GetComponent(
                    childId,
                    typeof(TransformParent2D)).BoxedValue;

            Assert.Equal(
                new EntityId(rootId.Value, 0),
                childParent.Parent);

            var grandchildParent = (TransformParent2D)
                document.GetComponent(
                    grandchildId,
                    typeof(TransformParent2D)).BoxedValue;

            Assert.Equal(
                new EntityId(childId.Value, 0),
                grandchildParent.Parent);

            document.DeleteEntity(childId);

            Assert.Single(document.Asset.Entities);
            Assert.True(document.IsDirty);

            document.Undo();

            Assert.Equal(3, document.Asset.Entities.Count);

            document.Redo();

            Assert.Single(document.Asset.Entities);

            document.Undo();

            document.Save();

            Assert.False(document.IsDirty);

            var restored = saveLoad.Load(path);

            Assert.Equal(3, restored.Entities.Count);
            Assert.Equal(rootId, restored.RootEntity);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void DeleteEntity_RootEntity_Throws()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "Test.prefab");

        try
        {
            var registry = new EcsComponentSerializerRegistry();
            var saveLoad = new PrefabAssetSaveLoadService(registry);

            var rootId = new PrefabEntityId(1);

            var asset = new PrefabAsset(
                "Test",
                rootId,
                new[]
                {
                new PrefabEntity(
                    rootId,
                    Array.Empty<PrefabComponent>())
                });

            saveLoad.Save(path, asset);

            var document = PrefabDocument.Open(path, saveLoad);
            var originalAsset = document.Asset;

            var exception = Assert.Throws<InvalidOperationException>(
                () => document.DeleteEntity(rootId));

            Assert.Equal(
                "The root entity cannot be deleted.",
                exception.Message);

            Assert.Same(originalAsset, document.Asset);
            Assert.Single(document.Asset.Entities);
            Assert.False(document.IsDirty);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}