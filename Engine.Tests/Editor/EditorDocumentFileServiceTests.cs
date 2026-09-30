using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorDocumentFileServiceTests
{
    [Fact]
    public void Save_RequiresFilePath()
    {
        var editor =
            new EditorContext();

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var document =
            editor.OpenDocument(
                world);

        var persistence =
            new TestPersistence();

        var service =
            new EditorDocumentFileService(
                editor,
                persistence);

        Assert.Throws<InvalidOperationException>(
            () =>
                service.Save(
                    document));
    }

    [Fact]
    public void SaveAs_AssignsNormalizedPathAndMarksSaved()
    {
        var editor =
            new EditorContext();

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var document =
            editor.OpenDocument(
                world);

        var persistence =
            new TestPersistence();

        var service =
            new EditorDocumentFileService(
                editor,
                persistence);

        var path =
            Path.Combine(
                "Documents",
                "..",
                "test.world");

        service.SaveAs(
            document,
            path);

        Assert.Equal(
            Path.GetFullPath(path),
            document.FilePath);

        Assert.False(
            document.IsDirty);

        Assert.Equal(
            Path.GetFullPath(path),
            persistence.SavedPath);
    }

    [Fact]
    public void Open_RejectsAlreadyOpenFile()
    {
        var editor =
            new EditorContext();

        var persistence =
            new TestPersistence();

        var service =
            new EditorDocumentFileService(
                editor,
                persistence);

        var first =
            service.Open(
                "test.world");

        Assert.Throws<InvalidOperationException>(
            () =>
                service.Open(
                    Path.GetFullPath(
                        "test.world")));
    }

    [Fact]
    public void SaveAs_RejectsPathOwnedByAnotherDocument()
    {
        var editor =
            new EditorContext();

        var persistence =
            new TestPersistence();

        var service =
            new EditorDocumentFileService(
                editor,
                persistence);

        var first =
            service.Open(
                "first.world");

        var second =
            service.Open(
                "second.world");

        Assert.Throws<InvalidOperationException>(
            () =>
                service.SaveAs(
                    second,
                    first.FilePath!));
    }

    private sealed class TestPersistence :
        IEditorDocumentPersistence
    {
        public string? SavedPath { get; private set; }

        public void Save(
            string path,
            EditorDocument document)
        {
            SavedPath =
                path;
        }

        public World Load(
            string path)
        {
            var ecsWorld =
                new Engine.ECS.World();

            var world =
                new World(
                    new ChunkSize(16, 16),
                    ecsWorld);

            return world;
        }
    }
}