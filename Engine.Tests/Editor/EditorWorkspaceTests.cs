using Engine.Editor.Documents;
using Engine.Editor.Panels;
using Engine.Editor.Workspace;

namespace Engine.Tests.Editor;

public sealed class EditorWorkspaceTests
{
    [Fact]
    public void RegisterPanel_WhenLayoutAlreadyContainsId_DoesNotModifyPanels()
    {
        var session =
            new EditorSession();

        var workspace =
            new EditorWorkspace(
                session);

        workspace.Layout.RegisterPanel(
            "Inspector");

        var panel =
            new TestPanel(
                "Inspector");

        Assert.Throws<InvalidOperationException>(
            () =>
                workspace.RegisterPanel(
                    panel));

        Assert.Empty(
            workspace.Panels);
    }

    [Fact]
    public void PanelLayout_RejectsInvalidDockArea()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new EditorPanelLayout(
                    "Inspector",
                    (EditorDockArea)255));
    }

    private sealed class TestPanel :
        IEditorPanel
    {
        public TestPanel(
            string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string Title =>
            Id;

        public bool IsOpen { get; set; }
    }
}