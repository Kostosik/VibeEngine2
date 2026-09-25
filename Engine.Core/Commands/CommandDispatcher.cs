namespace Engine.Core.Commands;

public sealed class CommandDispatcher
{
    public event Action<ICommand>? CommandDispatched;

    private readonly CommandQueue _queue;

    private readonly Dictionary<
        Type,
        Action<ICommand>> _handlers = new();

    public CommandDispatcher(
        CommandQueue queue)
    {
        ArgumentNullException.ThrowIfNull(
            queue);

        _queue = queue;
    }

    public int PendingCount =>
        _queue.Count;

    public CommandHandlerSubscription Register<TCommand>(
        ICommandHandler<TCommand> handler)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(
            handler);

        var commandType =
            typeof(TCommand);

        if (_handlers.ContainsKey(
                commandType))
        {
            throw new InvalidOperationException(
                $"A handler for command " +
                $"'{commandType.Name}' is already registered.");
        }

        Action<ICommand> wrapper =
            command =>
                handler.Handle(
                    (TCommand)command);

        _handlers.Add(
            commandType,
            wrapper);

        return new CommandHandlerSubscription(
            () =>
                Unregister(
                    commandType,
                    wrapper));
    }

    public void DispatchPending()
    {
        while (_queue.TryPeek(
            out var command))
        {
            ArgumentNullException.ThrowIfNull(
                command);

            var commandType =
                command.GetType();

            if (!_handlers.TryGetValue(
                    commandType,
                    out var handler))
            {
                throw new InvalidOperationException(
                    $"No handler is registered for command " +
                    $"'{commandType.Name}'.");
            }

            _queue.TryDequeue(
                out var dequeuedCommand);

            ArgumentNullException.ThrowIfNull(
                dequeuedCommand);

            handler(dequeuedCommand);

            CommandDispatched?.Invoke(
            dequeuedCommand);
        }
    }

    public void ClearPending()
    {
        _queue.Clear();
    }

    private void Unregister(
        Type commandType,
        Action<ICommand> handler)
    {
        if (!_handlers.TryGetValue(
                commandType,
                out var registeredHandler))
        {
            return;
        }

        if (!ReferenceEquals(
                registeredHandler,
                handler))
        {
            return;
        }

        _handlers.Remove(
            commandType);
    }
}