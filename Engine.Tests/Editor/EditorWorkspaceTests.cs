using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Panels;
using Engine.Editor.Workspace;

namespace Engine.Tests.Editor;

public sealed class EditorWorkspaceTests
{
    [Fact]
    public void RestoreState_WhenPanelRejectsState_PreservesPreviousWorkspaceState()
    {
        var workspace = new EditorWorkspace(new EditorSession());

        var panel = new TestPanel("Inspector")
        {
            IsOpen = true
        };

        workspace.RegisterPanel(panel);
        workspace.SetPanelArea("Inspector", EditorDockArea.Left);
        workspace.SetPanelOrder("Inspector", 3);
        workspace.SetPanelSize("Inspector", 240.0f);
        workspace.SetPanelActive("Inspector", true);

        var previousState = workspace.CaptureState();

        panel.ThrowWhenClosed = true;

        var invalidState = new EditorWorkspaceState(
            new[]
            {
            new EditorPanelLayoutState(
                "Inspector",
                EditorDockArea.Right,
                4,
                400.0f,
                false,
                false)
            });

        Assert.Throws<IOException>(
            () => workspace.RestoreState(invalidState));

        Assert.Equal(
            previousState.Panels,
            workspace.CaptureState().Panels);

        Assert.True(panel.IsOpen);
    }

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
    public void ActivatePanel_DeactivatesOtherPanelInSameArea()
    {
        var editor =
            new EditorContext();

        editor.Workspace.ActivatePanel(
            "AssetBrowser");

        editor.Workspace.ActivatePanel(
            "Console");

        Assert.False(
            editor.Workspace.Layout.GetPanel(
                "AssetBrowser").IsActive);

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "Console").IsActive);
    }

    [Fact]
    public void SetPanelArea_WhenInactivePanelMoves_DoesNotDeactivateActivePanel()
    {
        var editor =
            new EditorContext();

        editor.Workspace.ActivatePanel(
            "Inspector");

        editor.Workspace.SetPanelActive(
            "AssetBrowser",
            false);

        editor.Workspace.SetPanelArea(
            "AssetBrowser",
            EditorDockArea.Right);

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "Inspector").IsActive);

        Assert.False(
            editor.Workspace.Layout.GetPanel(
                "AssetBrowser").IsActive);
    }

    [Fact]
    public void RestoreState_WhenMultiplePanelsAreActiveInSameArea_LeavesOnlyOneActive()
    {
        var session =
            new EditorSession();

        var workspace =
            new EditorWorkspace(
                session);

        workspace.RegisterPanel(
            new TestPanel("First"));

        workspace.RegisterPanel(
            new TestPanel("Second"));

        var state =
            new EditorWorkspaceState(
                new[]
                {
                new EditorPanelLayoutState(
                    "First",
                    EditorDockArea.Center,
                    0,
                    0.0f,
                    true),

                new EditorPanelLayoutState(
                    "Second",
                    EditorDockArea.Center,
                    1,
                    0.0f,
                    true)
                });

        workspace.RestoreState(
            state);

        Assert.True(
            workspace.Layout.GetPanel(
                "First").IsActive);

        Assert.False(
            workspace.Layout.GetPanel(
                "Second").IsActive);
    }

    [Fact]
    public void SetPanelArea_WhenActivePanelMovesToOccupiedArea_DeactivatesOtherActivePanel()
    {
        var editor =
            new EditorContext();

        editor.Workspace.ActivatePanel(
            "AssetBrowser");

        editor.Workspace.ActivatePanel(
            "Inspector");

        editor.Workspace.SetPanelArea(
            "AssetBrowser",
            EditorDockArea.Right);

        Assert.Equal(
            EditorDockArea.Right,
            editor.Workspace.Layout.GetPanel(
                "AssetBrowser").Area);

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "AssetBrowser").IsActive);

        Assert.False(
            editor.Workspace.Layout.GetPanel(
                "Inspector").IsActive);
    }

    [Fact]
    public void ActivatePanel_DoesNotAffectOtherAreas()
    {
        var editor =
            new EditorContext();

        editor.Workspace.ActivatePanel(
    "Inspector");

        editor.Workspace.ActivatePanel(
            "AssetBrowser");

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "AssetBrowser").IsActive);

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "Inspector").IsActive);

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "DocumentArea").IsActive);
    }

    [Fact]
    public void ClosingViewport_DoesNotCloseDocumentArea()
    {
        var editor =
            new EditorContext();

        editor.Workspace.SetPanelOpen(
            "Viewport",
            false);

        Assert.False(
            editor.Workspace.FindPanel(
                "Viewport")!.IsOpen);

        Assert.True(
            editor.Workspace.FindPanel(
                "DocumentArea")!.IsOpen);

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "DocumentArea").IsActive);
    }

    [Fact]
    public void ActivatePanel_RaisesChangedOnlyWhenStateChanges()
    {
        var editor =
            new EditorContext();

        var changed =
            0;

        editor.Workspace.Changed +=
            () => changed++;

        editor.Workspace.ActivatePanel(
            "DocumentArea");

        Assert.Equal(
            0,
            changed);

        editor.Workspace.ActivatePanel(
            "Inspector");

        Assert.Equal(
            1,
            changed);

        editor.Workspace.ActivatePanel(
            "Inspector");

        Assert.Equal(
            1,
            changed);
    }

    [Fact]
    public void ActivatePanel_WhenPanelDoesNotExist_Throws()
    {
        var editor =
            new EditorContext();

        Assert.Throws<KeyNotFoundException>(
            () =>
                editor.Workspace.ActivatePanel(
                    "Missing"));
    }

    [Fact]
    public void SetPanelActive_False_DeactivatesPanel()
    {
        var editor =
            new EditorContext();

        editor.Workspace.SetPanelActive(
            "Viewport",
            false);

        Assert.False(
            editor.Workspace.Layout.GetPanel(
                "Viewport").IsActive);
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
        "DocumentArea"));

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "Hierarchy"));

        Assert.True(
            editor.Workspace.Layout.GetPanel(
                "DocumentArea").IsActive);

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "Inspector"));

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "AssetBrowser"));

        Assert.NotNull(
            editor.Workspace.FindPanel(
                "Console"));
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

        public bool ThrowWhenClosed { get; set; }

        public void SetOpen(bool isOpen)
        {
            if (!isOpen && ThrowWhenClosed)
            {
                throw new IOException("Simulated panel restore failure.");
            }

            IsOpen = isOpen;
        }
    }
}