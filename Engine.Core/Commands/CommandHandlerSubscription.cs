namespace Engine.Core.Commands;

public sealed class CommandHandlerSubscription : IDisposable
{
    private Action? _unsubscribe;

    internal CommandHandlerSubscription(
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