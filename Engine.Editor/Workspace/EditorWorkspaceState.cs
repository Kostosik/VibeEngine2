namespace Engine.Editor.Workspace;

public sealed class EditorWorkspaceState
{
    public EditorWorkspaceState(
        IReadOnlyList<EditorPanelLayoutState> panels)
    {
        ArgumentNullException.ThrowIfNull(
            panels);

        Panels =
            panels.ToArray();
    }

    public IReadOnlyList<EditorPanelLayoutState> Panels { get; }
}