using Engine.Editor;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorDocumentFileServiceTests
{
    [Fact]
    public void SaveAs_WhenPersistenceFails_PreservesDocumentStateAndAllowsRetry()
    {
        var editor = new EditorContext();

        using var ecsWorld = new Engine.ECS.World();

        var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        using var document = editor.OpenDocument(world);

        document.Execute(
            new CreateEditorEntityCommand(document));

        var persistence = new TestPersistence
        {
            FailNextSave = true
        };

        var service = new EditorDocumentFileService(
            editor,
            persistence);

        var path = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineSave-{Guid.NewGuid():N}.world");

        Assert.True(document.IsDirty);
        Assert.Null(document.FilePath);

        Assert.Throws<IOException>(
            () => service.SaveAs(document, path));

        // Неудачная запись не должна помечать документ сохранённым.
        Assert.True(document.IsDirty);

        // Путь не должен назначаться до успешного сохранения.
        Assert.Null(document.FilePath);
        Assert.Null(persistence.SavedPath);

        // После устранения ошибки повторное сохранение работает.
        service.SaveAs(document, path);

        Assert.Equal(
            Path.GetFullPath(path),
            document.FilePath);

        Assert.False(document.IsDirty);

        Assert.Equal(
            Path.GetFullPath(path),
            persistence.SavedPath);
    }

    [Fact]
    public void Open_WhenPersistenceLoadFails_DoesNotRegisterDocument()
    {
        var editor = new EditorContext();

        var persistence = new TestPersistence
        {
            FailNextLoad = true
        };

        var service = new EditorDocumentFileService(
            editor,
            persistence);

        Assert.Throws<IOException>(
            () => service.Open("broken.world"));

        Assert.Empty(editor.Session.Documents);
        Assert.Null(editor.ActiveDocument);
    }

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
        public bool FailNextLoad { get; set; }
        public bool FailNextSave { get; set; }

        public void Save(
            string path,
            EditorDocument document)
        {
            if (FailNextSave)
            {
                FailNextSave = false;

                throw new IOException(
                    "Simulated write failure.");
            }

            SavedPath = path;
        }

        public World Load(string path)
        {
            if (FailNextLoad)
            {
                FailNextLoad = false;
                throw new IOException("Simulated read failure.");
            }

            var ecsWorld = new Engine.ECS.World();

            var world = new World(
                new ChunkSize(16, 16),
                ecsWorld);

            return world;
        }
    }
}