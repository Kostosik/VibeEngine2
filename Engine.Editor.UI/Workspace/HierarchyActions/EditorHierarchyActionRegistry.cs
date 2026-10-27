namespace Engine.Editor.UI.Workspace;

public sealed class EditorHierarchyActionRegistry
{
    private readonly Dictionary<
        string,
        EditorHierarchyActionDefinition> _actions =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<EditorHierarchyActionDefinition>
        Actions =>
        _actions.Values.ToArray();

    public void Register(
        EditorHierarchyActionDefinition action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_actions.ContainsKey(action.Id))
        {
            throw new InvalidOperationException(
                $"Hierarchy action '{action.Id}' is already registered.");
        }

        _actions.Add(action.Id, action);
    }

    public bool Unregister(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return _actions.Remove(id);
    }

    public bool TryGet(
        string id,
        out EditorHierarchyActionDefinition? action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return _actions.TryGetValue(id, out action);
    }

    public IReadOnlyList<EditorHierarchyActionDefinition>
        GetForSurface(EditorHierarchyActionSurface surface)
    {
        return _actions.Values
            .Where(action =>
                (action.Surfaces & surface) !=
                EditorHierarchyActionSurface.None)
            .OrderBy(action => action.Order)
            .ThenBy(
                action => action.Id,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool CanExecute(
        string id,
        EditorHierarchyActionContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(context);

        return GetRequired(id).CanExecute(context);
    }

    public bool Execute(
        string id,
        EditorHierarchyActionContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(context);

        var action = GetRequired(id);

        if (!action.CanExecute(context))
        {
            return false;
        }

        action.Invoke(context);
        return true;
    }

    private EditorHierarchyActionDefinition GetRequired(string id)
    {
        return _actions.TryGetValue(id, out var action)
            ? action
            : throw new KeyNotFoundException(
                $"Hierarchy action '{id}' is not registered.");
    }
}