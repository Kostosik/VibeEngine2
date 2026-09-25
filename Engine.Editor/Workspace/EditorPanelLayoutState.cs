namespace Engine.Editor.Workspace;

public sealed record EditorPanelLayoutState(
    string PanelId,
    EditorDockArea Area,
    int Order,
    float Size,
    bool IsActive);