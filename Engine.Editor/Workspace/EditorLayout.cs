namespace Engine.Editor.Workspace;

public sealed class EditorLayout
{
    private readonly Dictionary<string, EditorPanelLayout> _panels =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<EditorPanelLayout> Panels =>
        _panels.Values;

    public void RegisterPanel(
        string panelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            panelId);

        if (_panels.ContainsKey(panelId))
        {
            throw new InvalidOperationException(
                $"Layout already contains panel '{panelId}'.");
        }

        _panels.Add(
            panelId,
            new EditorPanelLayout(
                panelId));
    }

    public bool RemovePanel(
        string panelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            panelId);

        return _panels.Remove(
            panelId);
    }

    public EditorPanelLayout GetPanel(
        string panelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            panelId);

        if (!_panels.TryGetValue(
                panelId,
                out var layout))
        {
            throw new KeyNotFoundException(
                $"Panel '{panelId}' is not registered in the layout.");
        }

        return layout;
    }

    public EditorWorkspaceState CaptureState()
    {
        return new EditorWorkspaceState(
            _panels.Values
                .OrderBy(
                    panel => panel.PanelId,
                    StringComparer.Ordinal)
                .Select(
                    panel =>
                        new EditorPanelLayoutState(
                            panel.PanelId,
                            panel.Area,
                            panel.Order,
                            panel.Size,
                            panel.IsActive))
                .ToArray());
    }

    public void RestoreState(
    EditorWorkspaceState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        foreach (var savedPanel in state.Panels)
        {
            if (!_panels.TryGetValue(
                    savedPanel.PanelId,
                    out var panel))
            {
                continue;
            }

            panel.SetArea(
                savedPanel.Area);

            panel.SetOrder(
                savedPanel.Order);

            panel.SetSize(
                savedPanel.Size);

            panel.SetActive(
                savedPanel.IsActive);
        }
    }

    public bool TryGetPanel(
        string panelId,
        out EditorPanelLayout? layout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            panelId);

        return _panels.TryGetValue(
            panelId,
            out layout);
    }
}