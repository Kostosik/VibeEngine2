using Engine.Editor.Documents;
using Engine.Editor.Panels;

namespace Engine.Editor.Workspace;

public sealed class EditorWorkspace
{
    private readonly List<IEditorPanel> _panels = new();

    public EditorLayout Layout { get; }

    public IReadOnlyList<IEditorPanel> Panels =>
        _panels;

    public EditorSession Session { get; }

    public event Action? Changed;

    public EditorWorkspace(
        EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(
            session);

        Layout =
            new EditorLayout();

        Session =
            session;
    }

    public void RegisterPanel(
        IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(
            panel);

        if (_panels.Any(
                existing =>
                    string.Equals(
                        existing.Id,
                        panel.Id,
                        StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Editor panel '{panel.Id}' is already registered.");
        }

        if (Layout.TryGetPanel(
                panel.Id,
                out _))
        {
            throw new InvalidOperationException(
                $"Editor panel '{panel.Id}' is already registered in the layout.");
        }

        Layout.RegisterPanel(
            panel.Id);

        _panels.Add(
            panel);

        Changed?.Invoke();
    }

    public bool UnregisterPanel(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        for (var i = 0; i < _panels.Count; i++)
        {
            if (!string.Equals(
                    _panels[i].Id,
                    id,
                    StringComparison.Ordinal))
            {
                continue;
            }

            _panels.RemoveAt(i);

            Layout.RemovePanel(
                id);

            Changed?.Invoke();

            return true;
        }

        return false;
    }

    public IEditorPanel? FindPanel(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        return _panels.FirstOrDefault(
            panel =>
                string.Equals(
                    panel.Id,
                    id,
                    StringComparison.Ordinal));
    }

    public void SetPanelOpen(
        string id,
        bool isOpen)
    {
        var panel =
            GetPanelOrThrow(id);

        if (panel.IsOpen == isOpen)
        {
            return;
        }

        panel.IsOpen =
            isOpen;

        Changed?.Invoke();
    }

    public void SetPanelArea(
        string id,
        EditorDockArea area)
    {
        var layout =
            Layout.GetPanel(id);

        if (layout.Area == area)
        {
            return;
        }

        layout.SetArea(
            area);

        Changed?.Invoke();
    }

    public void SetPanelOrder(
        string id,
        int order)
    {
        var layout =
            Layout.GetPanel(id);

        if (layout.Order == order)
        {
            return;
        }

        layout.SetOrder(
            order);

        Changed?.Invoke();
    }

    public void SetPanelSize(
        string id,
        float size)
    {
        var layout =
            Layout.GetPanel(id);

        if (layout.Size == size)
        {
            return;
        }

        layout.SetSize(
            size);

        Changed?.Invoke();
    }

    public void SetPanelActive(
        string id,
        bool active)
    {
        var layout =
            Layout.GetPanel(id);

        if (layout.IsActive == active)
        {
            return;
        }

        layout.SetActive(
            active);

        Changed?.Invoke();
    }

    public EditorWorkspaceState CaptureState()
    {
        var layoutState =
            Layout.CaptureState();

        var panels =
            layoutState.Panels
                .Select(
                    layout =>
                    {
                        var panel =
                            FindPanel(
                                layout.PanelId)
                            ?? throw new InvalidOperationException(
                                $"Editor panel '{layout.PanelId}' is missing.");

                        return layout with
                        {
                            IsOpen =
                                panel.IsOpen
                        };
                    })
                .ToArray();

        return new EditorWorkspaceState(
            panels);
    }

    public void RestoreState(
        EditorWorkspaceState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        Layout.RestoreState(
            state);

        foreach (var savedPanel in state.Panels)
        {
            var panel =
                FindPanel(
                    savedPanel.PanelId);

            if (panel is null)
            {
                continue;
            }

            panel.IsOpen =
                savedPanel.IsOpen;
        }

        Changed?.Invoke();
    }

    private IEditorPanel GetPanelOrThrow(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        return FindPanel(id)
            ?? throw new KeyNotFoundException(
                $"Editor panel '{id}' was not found.");
    }
}