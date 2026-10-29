using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.Persistence;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorSessionPersistenceTests
{
    [Fact]
    public void LoadSession_WhenRestoreFails_PreservesPreviouslyOpenDocuments()
    {
        var editor = new EditorContext();

        var existingDocument = editor.OpenDocument(
            CreateWorld(),
            Path.GetFullPath("existing.world"));

        var state = new EditorSessionState(
            new[]
            {
            Path.GetFullPath("first.world"),
            Path.GetFullPath("second.world")
            },
            Path.GetFullPath("first.world"));

        var persistence = new TestSessionPersistence(state);
        var files = new FailingDocumentFileService(editor);

        Assert.Throws<IOException>(
            () => editor.LoadSession(
                persistence,
                files,
                "session.json"));

        Assert.Single(editor.Session.Documents);

        Assert.Same(
            existingDocument,
            editor.Session.Documents[0]);

        Assert.Same(
            existingDocument,
            editor.ActiveDocument);
    }

    [Fact]
    public void LoadSession_WhenDocumentRestoreFails_ClearsPartiallyRestoredSession()
    {
        var editor = new EditorContext();

        var firstPath = Path.GetFullPath("first.world");
        var secondPath = Path.GetFullPath("second.world");

        var state = new EditorSessionState(
            new[] { firstPath, secondPath },
            firstPath);

        var persistence = new TestSessionPersistence(state);
        var files = new FailingDocumentFileService(editor);

        Assert.Throws<IOException>(
            () => editor.LoadSession(
                persistence,
                files,
                "session.json"));

        Assert.Empty(editor.Session.Documents);
        Assert.Empty(editor.Session.AllDocuments);
        Assert.Null(editor.ActiveDocument);
        Assert.Null(editor.Session.ActiveEditorDocument);
    }

    [Fact]
    public void CaptureSessionState_StoresOpenFilePathsAndActiveDocument()
    {
        var editor =
            new EditorContext();

        var first =
            editor.OpenDocument(
                CreateWorld(),
                Path.GetFullPath(
                    "first.world"));

        var second =
            editor.OpenDocument(
                CreateWorld(),
                Path.GetFullPath(
                    "second.world"));

        editor.Session.Activate(
            first);

        var state =
            editor.CaptureSessionState();

        Assert.Equal(
            new[]
            {
                Path.GetFullPath("first.world"),
                Path.GetFullPath("second.world")
            },
            state.DocumentPaths);

        Assert.Equal(
            Path.GetFullPath("first.world"),
            state.ActiveDocumentPath);

        editor.Session.DiscardAll();
    }

    [Fact]
    public void JsonPersistence_RoundTripsSessionState()
    {
        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.json");

        try
        {
            var state =
                new EditorSessionState(
                    new[]
                    {
                        Path.GetFullPath("first.world"),
                        Path.GetFullPath("second.world")
                    },
                    Path.GetFullPath("second.world"));

            var persistence =
                new JsonEditorSessionPersistence();

            persistence.Save(
                path,
                state);

            var loaded =
                persistence.Load(
                    path);

            Assert.Equal(
                state.DocumentPaths,
                loaded.DocumentPaths);

            Assert.Equal(
                state.ActiveDocumentPath,
                loaded.ActiveDocumentPath);
        }
        finally
        {
            File.Delete(
                path);
        }
    }

    [Fact]
    public void CaptureSessionState_IgnoresUntitledDocument()
    {
        var editor =
            new EditorContext();

        var world =
            CreateWorld();

        editor.OpenDocument(
            world);

        var state =
            editor.CaptureSessionState();

        Assert.Empty(
            state.DocumentPaths);

        Assert.Null(
            state.ActiveDocumentPath);

        editor.Session.DiscardAll();
    }

    private static World CreateWorld()
    {
        return new World(
            new ChunkSize(16, 16),
            new Engine.ECS.World());
    }

    private sealed class TestSessionPersistence
    : IEditorSessionPersistence
    {
        private readonly EditorSessionState _state;

        public TestSessionPersistence(EditorSessionState state)
        {
            _state = state;
        }

        public void Save(string path, EditorSessionState state)
        {
        }

        public EditorSessionState Load(string path)
        {
            return _state;
        }
    }

    private sealed class FailingDocumentFileService
        : IEditorDocumentFileService
    {
        private readonly EditorContext _editor;
        private int _openCount;

        public FailingDocumentFileService(EditorContext editor)
        {
            _editor = editor;
        }

        public EditorDocument Open(string path)
        {
            _openCount++;

            if (_openCount == 2)
            {
                throw new IOException(
                    "Simulated document restore failure.");
            }

            var world = new World(
                new ChunkSize(16, 16),
                new Engine.ECS.World());

            return _editor.OpenDocument(world, path);
        }

        public void Save(EditorDocument document)
        {
            throw new NotSupportedException();
        }

        public void SaveAs(EditorDocument document, string path)
        {
            throw new NotSupportedException();
        }
    }
}