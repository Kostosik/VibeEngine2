namespace Engine.Core.Events;

public sealed class EventSubscription : IDisposable
{
    private Action? _unsubscribe;

    internal EventSubscription(
        Action unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(
            unsubscribe);

        _unsubscribe =
            unsubscribe;
    }

    public void Dispose()
    {
        var unsubscribe =
            _unsubscribe;

        if (unsubscribe is null)
        {
            return;
        }

        _unsubscribe = null;

        unsubscribe();
    }
}