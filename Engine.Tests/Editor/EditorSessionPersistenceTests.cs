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
}