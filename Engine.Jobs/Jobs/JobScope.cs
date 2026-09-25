using System.Runtime.ExceptionServices;
using Engine.Jobs.Scheduling;

namespace Engine.Jobs.Jobs;

public sealed class JobScope
{
    [ThreadStatic]
    private static JobScope? _current;

    private readonly JobScheduler _scheduler;

    private readonly List<JobHandle> _handles =
        new();

    internal JobScope(
        JobScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(
            scheduler);

        _scheduler =
            scheduler;
    }

    public static JobScope? Current =>
        _current;

    public void Track(
        JobHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        if (!_scheduler.Owns(
                handle))
        {
            throw new InvalidOperationException(
                "Job handle belongs to a different scheduler.");
        }

        _handles.Add(
            handle);
    }

    public void Wait()
    {
        if (_handles.Count == 0)
        {
            return;
        }

        var handles =
            _handles.ToArray();

        _handles.Clear();

        ExceptionDispatchInfo? failure =
            null;

        foreach (var handle in handles)
        {
            try
            {
                _scheduler.Wait(
                    handle);
            }
            catch (Exception exception)
            {
                failure ??=
                    ExceptionDispatchInfo.Capture(
                        exception);
            }
        }

        failure?.Throw();
    }

    public IDisposable Enter()
    {
        var previous =
            _current;

        _current =
            this;

        return new ScopeToken(
            previous);
    }

    private sealed class ScopeToken :
        IDisposable
    {
        private readonly JobScope? _previous;

        private bool _disposed;

        public ScopeToken(
            JobScope? previous)
        {
            _previous =
                previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _current =
                _previous;

            _disposed =
                true;
        }
    }
}