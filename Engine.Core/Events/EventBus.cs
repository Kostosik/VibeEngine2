namespace Engine.Core.Events;

public sealed class EventBus : IDisposable
{
    private readonly Queue<IEvent> _events = new();

    private readonly Dictionary<
        Type,
        List<Action<IEvent>>> _handlers = new();
    private bool _disposed;
    public int PendingCount =>
        _events.Count;

    public EventSubscription Subscribe<TEvent>(
        Action<TEvent> handler)
        where TEvent : IEvent
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        var eventType =
            typeof(TEvent);

        if (!_handlers.TryGetValue(
                eventType,
                out var handlers))
        {
            handlers =
                new List<Action<IEvent>>();

            _handlers.Add(
                eventType,
                handlers);
        }

        Action<IEvent> wrapper =
            @event =>
                handler((TEvent)@event);

        handlers.Add(
            wrapper);

        return new EventSubscription(
            () =>
                Unsubscribe(
                    eventType,
                    wrapper));
    }

    public void Publish<TEvent>(
        TEvent @event)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        ObjectDisposedException.ThrowIf(_disposed, this);
        _events.Enqueue(
            @event);
    }

    public void DispatchPending()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        while (_events.TryDequeue(
            out var @event))
        {
            Dispatch(@event);
        }
    }

    public void ClearSubscriptions()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _handlers.Clear();
    }

    public void ClearPending()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _events.Clear();
    }

    private void Dispatch(
        IEvent @event)
    {
        var eventType =
            @event.GetType();

        if (!_handlers.TryGetValue(
                eventType,
                out var handlers))
        {
            return;
        }

        var snapshot =
            handlers.ToArray();

        foreach (var handler in snapshot)
        {
            handler(@event);
        }
    }

    private void Unsubscribe(
        Type eventType,
        Action<IEvent> handler)
    {
        if (!_handlers.TryGetValue(
                eventType,
                out var handlers))
        {
            return;
        }

        handlers.Remove(handler);

        if (handlers.Count == 0)
        {
            _handlers.Remove(eventType);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _events.Clear();
        _handlers.Clear();

        _disposed = true;
    }
}