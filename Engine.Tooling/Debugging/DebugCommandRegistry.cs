namespace Engine.Tooling.Debugging;

public sealed class DebugCommandRegistry
{
    private readonly Dictionary<
        string,
        IDebugCommand> _commands =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(
        IDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException(
                "Command name cannot be empty.",
                nameof(command));
        }

        if (!_commands.TryAdd(
                command.Name,
                command))
        {
            throw new InvalidOperationException(
                $"Debug command '{command.Name}' is already registered.");
        }
    }

    public bool Unregister(
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _commands.Remove(name);
    }

    public bool TryGet(
        string name,
        out IDebugCommand? command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _commands.TryGetValue(
            name,
            out command);
    }

    public IReadOnlyCollection<IDebugCommand> Commands =>
        _commands.Values;
}