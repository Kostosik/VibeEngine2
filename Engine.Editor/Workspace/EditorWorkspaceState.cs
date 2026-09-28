namespace Engine.Editor.Workspace;

public sealed class EditorWorkspaceState
{
    public EditorWorkspaceState(
        IReadOnlyList<EditorPanelLayoutState> panels)
    {
        ArgumentNullException.ThrowIfNull(
            panels);

        var copy =
            panels.ToArray();

        var ids =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var panel in copy)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                panel.PanelId);

            if (!ids.Add(
                    panel.PanelId))
            {
                throw new ArgumentException(
                    $"Workspace state contains duplicate panel '{panel.PanelId}'.",
                    nameof(panels));
            }

            if (!Enum.IsDefined(
                    typeof(EditorDockArea),
                    panel.Area))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(panels),
                    $"Panel '{panel.PanelId}' contains an invalid dock area.");
            }

            if (panel.Order < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(panels),
                    $"Panel '{panel.PanelId}' contains a negative order.");
            }

            if (panel.Size < 0.0f ||
                float.IsNaN(panel.Size) ||
                float.IsInfinity(panel.Size))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(panels),
                    $"Panel '{panel.PanelId}' contains an invalid size.");
            }
        }

        Panels =
            copy;
    }

    public IReadOnlyList<EditorPanelLayoutState> Panels { get; }
}