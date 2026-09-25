namespace Engine.Core.Diagnostics.Metrics;

public sealed class MetricCollector
{
    private readonly Dictionary<string, Metric> _metrics = new();

    public IReadOnlyCollection<Metric> Metrics =>
        _metrics.Values;

    public void Increment(
        string name,
        double value = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_metrics.TryGetValue(name, out var metric))
        {
            _metrics[name] = metric with
            {
                Value = metric.Value + value
            };

            return;
        }

        _metrics[name] = new Metric(
            name,
            MetricType.Counter,
            value);
    }

    public void Set(
        string name,
        double value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _metrics[name] = new Metric(
            name,
            MetricType.Gauge,
            value);
    }

    public void RecordTime(
        string name,
        TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _metrics[name] = new Metric(
            name,
            MetricType.Timing,
            duration.TotalMilliseconds);
    }

    public bool TryGet(
        string name,
        out Metric metric)
    {
        return _metrics.TryGetValue(
            name,
            out metric);
    }

    public void Clear()
    {
        _metrics.Clear();
    }
}