using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Panels;
using Engine.Editor.Workspace;

namespace Engine.Tests.Editor;

public sealed class EditorWorkspaceTests
{
    [Fact]
    public void SetPanelOpen_RaisesChanged()
    {
        var session =
            new EditorSession();

        var workspace =
            new EditorWorkspace(
                session);

        var panel =
            new TestPanel(
                "Inspector")
            {
                IsOpen = true
            };

        workspace.RegisterPanel(
            panel);

        var changed =
            0;

        workspace.Changed +=
            () => changed++;

        workspace.SetPanelOpen(
            "Inspector",
            false);

        Assert.Equal(
            1,
            changed);

        Assert.False(
            panel.IsOpen);
    }

    [Fact]
    public void WorkspaceState_PreservesPanelOpenState()
    {
        var editor =
            new EditorContext();

        editor.Workspace.SetPanelOpen(
            "Inspector",
            false);

        var state =
            editor.Workspace.CaptureState();

        var inspector =
            state.Panels.Single(
                panel =>
                    panel.PanelId ==
                    "Inspector");

        Assert.False(
            inspector.IsOpen);

        var restored =
            new EditorContext();

        restored.Workspace.RestoreState(
            state);

        Assert.False(
            restored.Workspace.FindPanel(
                "Inspector")!
                .IsOpen);
    }

    [Fact]
    public void EditorContext_RegistersDefaultPanels()
    {
        var editor =
            new EditorContext();

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "Hierarchy"));

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "Viewport"));

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "Inspector"));

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "AssetBrowser"));

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "AssetPreview"));
    }

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

    [Fact]
    public void RegisterPanel_WhenPanelIdIsInvalid_DoesNotModifyPanels()
    {
        var session =
            new EditorSession();

        var workspace =
            new EditorWorkspace(
                session);

        var panel =
            new TestPanel(
                " ");

        Assert.Throws<ArgumentException>(
            () =>
                workspace.RegisterPanel(
                    panel));

        Assert.Empty(
            workspace.Panels);

        Assert.Empty(
            workspace.Layout.Panels);
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