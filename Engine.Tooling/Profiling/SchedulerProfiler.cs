using Engine.Core.Systems;

namespace Engine.Tooling.Profiling;

public sealed class SchedulerProfiler :
    IDisposable
{
    private readonly SystemScheduler _scheduler;
    private readonly IProfiler _profiler;

    private readonly Dictionary<
        Type,
        IDisposable> _scopes = new();

    private bool _disposed;

    public SchedulerProfiler(
        SystemScheduler scheduler,
        IProfiler profiler)
    {
        ArgumentNullException.ThrowIfNull(
            scheduler);

        ArgumentNullException.ThrowIfNull(
            profiler);

        _scheduler = scheduler;
        _profiler = profiler;

        _scheduler.SystemExecuting +=
            OnSystemExecuting;

        _scheduler.SystemExecuted +=
            OnSystemExecuted;
    }

    private void OnSystemExecuting(
        SystemRegistration registration)
    {
        if (_disposed)
            return;

        var name =
            $"{registration.Phase}." +
            $"{registration.SystemType.Name}";

        var scope =
            _profiler.BeginScope(
                name);

        _scopes.Add(
            registration.SystemType,
            scope);
    }

    private void OnSystemExecuted(
        SystemRegistration registration)
    {
        if (_disposed)
            return;

        if (_scopes.Remove(
                registration.SystemType,
                out var scope))
        {
            scope.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _scheduler.SystemExecuting -=
            OnSystemExecuting;

        _scheduler.SystemExecuted -=
            OnSystemExecuted;

        foreach (var scope in _scopes.Values)
        {
            scope.Dispose();
        }

        _scopes.Clear();

        _disposed = true;
    }
}