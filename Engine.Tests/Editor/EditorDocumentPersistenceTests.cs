using Engine.Editor;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.Types;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorDocumentPersistenceTests
{
    [Fact]
    public void SaveDocument_MarksDocumentSavedAndAssignsPath()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var editor =
            new EditorContext();

        var document =
            editor.OpenDocument(
                world);

        document.Execute(
            new CreateEditorEntityCommand(
                document));

        Assert.True(
            document.IsDirty);

        var persistence =
            new TestPersistence();

        editor.SaveDocument(
            document,
            persistence,
            "test.world");

        Assert.False(
            document.IsDirty);

        Assert.Equal(
            "test.world",
            document.FilePath);

        Assert.Same(
            document,
            persistence.SavedDocument);

        Assert.Equal(
            "test.world",
            persistence.SavedPath);
    }

    [Fact]
    public void LoadDocument_OpensWorldAndAssignsPath()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var persistence =
            new TestPersistence
            {
                WorldToLoad =
                    world
            };

        var editor =
            new EditorContext();

        var document =
            editor.LoadDocument(
                persistence,
                "loaded.world");

        Assert.Same(
            world,
            document.World);

        Assert.Equal(
            "loaded.world",
            document.FilePath);

        Assert.Same(
            document,
            editor.ActiveDocument);
    }

    [Fact]
    public void BinaryPersistence_RoundTripsSpatialEntity()
    {
        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"vibe-editor-{Guid.NewGuid():N}.world");

        try
        {
            var registry =
                new EcsComponentSerializerRegistry();

            registry.Register(
                "engine.world.position",
                new WorldPositionComponentSerializer());

            var persistence =
                new BinaryEditorDocumentPersistence(
                    registry);

            using var ecsWorld =
                new Engine.ECS.World();

            var world =
                new World(
                    new ChunkSize(16, 16),
                    ecsWorld);

            var entity =
                world.SpatialEntities.CreateEntity(
                    new WorldPosition(
                        -7,
                        11));

            persistence.Save(
                path,
                new EditorDocument(
                    world));

            var loadedWorld =
                persistence.Load(
                    path);

            Assert.True(
                loadedWorld.EcsWorld.Exists(
                    entity));

            Assert.Equal(
                new WorldPosition(
                    -7,
                    11),
                loadedWorld.SpatialEntities.GetPosition(
                    entity));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class TestPersistence :
        IEditorDocumentPersistence
    {
        public EditorDocument? SavedDocument { get; private set; }

        public string? SavedPath { get; private set; }

        public World? WorldToLoad { get; init; }

        public void Save(
            string path,
            EditorDocument document)
        {
            SavedPath = path;
            SavedDocument = document;
        }

        public World Load(
            string path)
        {
            return WorldToLoad
                ?? throw new InvalidOperationException(
                    "Test world has not been configured.");
        }
    }
}