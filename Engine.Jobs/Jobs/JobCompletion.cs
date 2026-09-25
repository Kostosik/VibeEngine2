using System.Runtime.ExceptionServices;

namespace Engine.Jobs.Jobs;

internal sealed class JobCompletion
{
    private const int Pending = 0;
    private const int Succeeded = 1;
    private const int Failed = 2;

    private readonly object _sync =
        new();

    private ExceptionDispatchInfo? _exception;

    private int _status;

    public bool IsCompleted =>
        Volatile.Read(
            ref _status) != Pending;

    public bool IsSuccessful =>
        Volatile.Read(
            ref _status) == Succeeded;

    public Exception? Failure
    {
        get
        {
            lock (_sync)
            {
                return _exception?.SourceException;
            }
        }
    }

    public bool Complete()
    {
        lock (_sync)
        {
            if (_status != Pending)
            {
                return false;
            }

            _status =
                Succeeded;

            Monitor.PulseAll(
                _sync);

            return true;
        }
    }

    public bool Fail(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(
            exception);

        lock (_sync)
        {
            if (_status != Pending)
            {
                return false;
            }

            _exception =
                ExceptionDispatchInfo.Capture(
                    exception);

            _status =
                Failed;

            Monitor.PulseAll(
                _sync);

            return true;
        }
    }

    public void Wait()
    {
        lock (_sync)
        {
            while (_status == Pending)
            {
                Monitor.Wait(
                    _sync);
            }

            _exception?.Throw();
        }
    }
}