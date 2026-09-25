namespace Engine.Core.Commands;

public sealed class CommandQueue
{
    private readonly Queue<ICommand> _commands = new();

    public int Count =>
        _commands.Count;

    public void Enqueue(
        ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _commands.Enqueue(command);
    }

    public bool TryPeek(
        out ICommand? command)
    {
        return _commands.TryPeek(
            out command);
    }

    public bool TryDequeue(
        out ICommand? command)
    {
        return _commands.TryDequeue(
            out command);
    }

    public void Clear()
    {
        _commands.Clear();
    }
}