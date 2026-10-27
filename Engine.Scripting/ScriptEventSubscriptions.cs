using Engine.Core.Events;

namespace Engine.Scripting;

internal sealed class ScriptEventSubscriptions : IDisposable
{
    private readonly EventBus _eventBus;

    private readonly List<EventSubscription> _subscriptions = new();

    private Action<Exception>? _faultHandler;

    private bool _active = true;
    private bool _disposed;

    public ScriptEventSubscriptions(EventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        _eventBus = eventBus;
    }

    public void SetFaultHandler(Action<Exception> handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        _faultHandler = handler;
    }

    public EventSubscription Subscribe<TEvent>(
        Action<TEvent> handler)
        where TEvent : IEvent
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = _eventBus.Subscribe<TEvent>(
            @event =>
            {
                if (!_active)
                {
                    return;
                }

                try
                {
                    handler(@event);
                }
                catch (Exception exception)
                {
                    _faultHandler?.Invoke(exception);
                }
            });

        _subscriptions.Add(subscription);

        return subscription;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _active = false;

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }
}