namespace Engine.UI.Actions;

public sealed class UiActionRegistry
{
    private readonly Dictionary<
        string,
        Action> _actions =
        new(
            StringComparer.Ordinal);

    public void Register(
        string id,
        Action action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        ArgumentNullException.ThrowIfNull(
            action);

        if (!_actions.TryAdd(
                id,
                action))
        {
            throw new InvalidOperationException(
                $"UI action '{id}' is already registered.");
        }
    }

    public bool Invoke(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        if (!_actions.TryGetValue(
                id,
                out var action))
        {
            return false;
        }

        action();

        return true;
    }

    public bool Unregister(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        return _actions.Remove(
            id);
    }

    public bool Contains(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        return _actions.ContainsKey(
            id);
    }
}