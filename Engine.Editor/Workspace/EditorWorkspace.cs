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

    public EditorWorkspace(
        EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(
            session);
        Layout =
    new EditorLayout();
        Session = session;
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

        _panels.Add(panel);
        Layout.RegisterPanel(
    panel.Id);
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
}