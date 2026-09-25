using Engine.Core.Diagnostics.Metrics;

namespace Engine.Tooling.Profiling;

public sealed class Profiler :
    IProfiler
{
    private readonly MetricCollector _metrics = new();

    public IReadOnlyCollection<Metric> Metrics =>
        _metrics.Metrics;

    public IDisposable BeginScope(
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ProfileScope(
            _metrics,
            name);
    }

    public void Clear()
    {
        _metrics.Clear();
    }

    private sealed class ProfileScope :
        IDisposable
    {
        private readonly MetricCollector _metrics;
        private readonly string _name;
        private readonly PerformanceTimer _timer;

        private bool _disposed;

        public ProfileScope(
            MetricCollector metrics,
            string name)
        {
            _metrics = metrics;
            _name = name;
            _timer = PerformanceTimer.Start();

            _metrics.Increment(
                $"{_name}.Calls");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _metrics.RecordTime(
                _name,
                _timer.Elapsed);
        }
    }
}