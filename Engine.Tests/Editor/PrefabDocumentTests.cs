using Engine.ECS.Prefabs;
using Engine.Editor.Documents;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Tests.Editor;

public sealed class PrefabDocumentTests
{
    [Fact]
    public void Open_LoadsPrefabAndCreatesReadOnlyDocument()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "Crate.prefab");

        try
        {
            var registry = new EcsComponentSerializerRegistry();

            var saveLoad = new PrefabAssetSaveLoadService(registry);

            var root = new PrefabEntityId(1);

            var asset = new PrefabAsset(
                "Crate",
                root,
                new[]
                {
                    new PrefabEntity(
                        root,
                        Array.Empty<PrefabComponent>())
                });

            saveLoad.Save(path, asset);

            var document = PrefabDocument.Open(
                path,
                saveLoad);

            Assert.Equal(PrefabDocument.TypeId, document.DocumentTypeId);
            Assert.Equal(path, document.FilePath);
            Assert.Equal("Crate.prefab", document.DisplayName);
            Assert.Equal("Crate", document.Asset.Name);
            Assert.Equal(root, document.Asset.RootEntity);
            Assert.False(document.IsDirty);

            document.Save();

            Assert.True(File.Exists(path));
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