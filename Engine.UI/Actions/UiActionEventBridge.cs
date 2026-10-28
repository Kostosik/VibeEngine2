using Engine.Core.Events;

namespace Engine.UI.Actions;

/// <summary>
/// Connects named UI actions to typed events on the shared EventBus.
/// </summary>
public sealed class UiActionEventBridge : IDisposable
{
    private readonly UiActionRegistry _actions;
    private readonly EventBus _events;

    private readonly HashSet<string> _registeredIds =
        new(StringComparer.Ordinal);

    private bool _disposed;

    public UiActionEventBridge(
        UiActionRegistry actions,
        EventBus events)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(events);

        _actions = actions;
        _events = events;
    }

    public void Register<TEvent>(
        string actionId,
        Func<TEvent> eventFactory)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(eventFactory);

        Register<TEvent>(
            actionId,
            _ => eventFactory());
    }

    public void Register<TEvent>(
        string actionId,
        Func<UiActionContext, TEvent> eventFactory)
        where TEvent : IEvent
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        ArgumentNullException.ThrowIfNull(eventFactory);

        _actions.Register(
            actionId,
            context =>
            {
                var @event = eventFactory(context);
                _events.Publish(@event);
            });

        _registeredIds.Add(actionId);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var actionId in _registeredIds)
        {
            _actions.Unregister(actionId);
        }

        _registeredIds.Clear();
    }
}