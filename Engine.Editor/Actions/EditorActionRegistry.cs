namespace Engine.Editor.Actions;

public sealed class EditorActionRegistry
{
    private readonly Dictionary<string, IEditorAction> _actions =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<IEditorAction> Actions =>
        _actions.Values;

    public void Register(
        IEditorAction action)
    {
        ArgumentNullException.ThrowIfNull(
            action);

        if (_actions.ContainsKey(
                action.Id))
        {
            throw new InvalidOperationException(
                $"Editor action '{action.Id}' is already registered.");
        }

        _actions.Add(
            action.Id,
            action);
    }

    public bool Unregister(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        return _actions.Remove(
            id);
    }

    public bool TryGet(
        string id,
        out IEditorAction? action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        return _actions.TryGetValue(
            id,
            out action);
    }

    public void Execute(
        string id,
        EditorActionContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        ArgumentNullException.ThrowIfNull(
            context);

        if (!_actions.TryGetValue(
                id,
                out var action))
        {
            throw new KeyNotFoundException(
                $"Editor action '{id}' is not registered.");
        }

        if (!action.CanExecute(
                context))
        {
            return;
        }

        action.Execute(
            context);
    }
}