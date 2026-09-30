using Engine.Editor;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorContextDocumentLifecycleTests
{
    [Fact]
    public void CloseDocument_Cancel_LeavesDocumentOpen()
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

        var result =
            editor.CloseDocument(
                document,
                EditorDocumentCloseDecision.Cancel);

        Assert.False(result);
        Assert.Contains(
            document,
            editor.Session.Documents);
    }

    [Fact]
    public void CloseDocument_Discard_ClosesDirtyDocument()
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

        document.Execute(
            new CreateEditorEntityCommand(
                document));

        Assert.True(
            document.IsDirty);

        var result =
            editor.CloseDocument(
                document,
                EditorDocumentCloseDecision.Discard);

        Assert.True(result);
        Assert.Empty(
            editor.Session.Documents);
    }

    [Fact]
    public void CloseDocument_Save_ClosesDocumentAfterSave()
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

        document.Execute(
            new CreateEditorEntityCommand(
                document));

        var persistence =
            new TestPersistence();

        var files =
            editor.CreateDocumentFileService(
                persistence);

        files.SaveAs(
            document,
            "test.world");

        document.Execute(
            new CreateEditorEntityCommand(
                document));

        Assert.True(
            document.IsDirty);

        var result =
            editor.CloseDocument(
                document,
                EditorDocumentCloseDecision.Save,
                files);

        Assert.True(result);
        Assert.Empty(
            editor.Session.Documents);
        Assert.Equal(
            Path.GetFullPath("test.world"),
            persistence.SavedPath);
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

            return new World(
                new ChunkSize(16, 16),
                ecsWorld);
        }
    }
}