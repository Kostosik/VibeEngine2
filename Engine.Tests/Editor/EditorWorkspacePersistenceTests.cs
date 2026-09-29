using Engine.Editor.Persistence;
using Engine.Editor.Workspace;

namespace Engine.Tests.Editor;

public sealed class EditorWorkspacePersistenceTests
{
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