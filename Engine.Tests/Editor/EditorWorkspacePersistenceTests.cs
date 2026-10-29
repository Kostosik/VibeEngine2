using Engine.Editor.Persistence;
using Engine.Editor.Workspace;

namespace Engine.Tests.Editor;

public sealed class EditorWorkspacePersistenceTests
{
    [Fact]
    public void Save_WhenReplacingLockedFileFails_PreservesOriginalAndRemovesTemporaryFile()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"WorkspacePersistence-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var path = Path.Combine(root, "workspace.json");
            const string original = """{"previous":"state"}""";

            File.WriteAllText(path, original);

            var state = new EditorWorkspaceState(
                new[]
                {
                new EditorPanelLayoutState(
                    "Inspector",
                    EditorDockArea.Right,
                    0,
                    280.0f,
                    false)
                });

            var persistence = new JsonEditorWorkspacePersistence();

            Exception? saveException;

            using (var lockedFile = new FileStream(
                       path,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.None))
            {
                saveException = Record.Exception(
                    () => persistence.Save(path, state));
            }

            Assert.NotNull(saveException);

            Assert.True(
                saveException is IOException
                    or UnauthorizedAccessException,
                $"Unexpected exception type: {saveException.GetType().FullName}");

            Assert.Equal(original, File.ReadAllText(path));

            Assert.Empty(
                Directory.EnumerateFiles(
                    root,
                    ".workspace.json.*.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsWorkspaceState()
    {
        var filePath =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.json");

        try
        {
            var state =
                new EditorWorkspaceState(
                    new[]
                    {
                        new EditorPanelLayoutState(
                            "Hierarchy",
                            EditorDockArea.Left,
                            2,
                            320.0f,
                            true),

                        new EditorPanelLayoutState(
                            "Inspector",
                            EditorDockArea.Right,
                            1,
                            280.0f,
                            false)
                    });

            var persistence =
                new JsonEditorWorkspacePersistence();

            persistence.Save(
                filePath,
                state);

            var loaded =
                persistence.Load(
                    filePath);

            Assert.Equal(
                state.Panels,
                loaded.Panels);
        }
        finally
        {
            File.Delete(
                filePath);
        }
    }
}