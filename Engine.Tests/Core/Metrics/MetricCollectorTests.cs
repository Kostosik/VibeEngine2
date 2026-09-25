using Engine.Core.Diagnostics.Metrics;

namespace Engine.Tests.Core.Diagnostics.Metrics;

public sealed class MetricCollectorTests
{
    [Fact]
    public void Increment_CreatesCounter()
    {
        var metrics = new MetricCollector();

        metrics.Increment("Entities.Created", 5);

        Assert.True(
            metrics.TryGet(
                "Entities.Created",
                out var metric));

        Assert.Equal(
            MetricType.Counter,
            metric.Type);

        Assert.Equal(
            5,
            metric.Value);
    }

    [Fact]
    public void Increment_AddsToExistingCounter()
    {
        var metrics = new MetricCollector();

        metrics.Increment("Entities.Created", 5);
        metrics.Increment("Entities.Created", 3);

        metrics.TryGet(
            "Entities.Created",
            out var metric);

        Assert.Equal(8, metric.Value);
    }

    [Fact]
    public void Set_CreatesGauge()
    {
        var metrics = new MetricCollector();

        metrics.Set("Entities.Active", 42);

        metrics.TryGet(
            "Entities.Active",
            out var metric);

        Assert.Equal(
            MetricType.Gauge,
            metric.Type);

        Assert.Equal(
            42,
            metric.Value);
    }

    [Fact]
    public void Set_ReplacesGaugeValue()
    {
        var metrics = new MetricCollector();

        metrics.Set("Entities.Active", 42);
        metrics.Set("Entities.Active", 100);

        metrics.TryGet(
            "Entities.Active",
            out var metric);

        Assert.Equal(100, metric.Value);
    }

    [Fact]
    public void RecordTime_StoresMilliseconds()
    {
        var metrics = new MetricCollector();

        metrics.RecordTime(
            "Simulation.Update",
            TimeSpan.FromMilliseconds(2.5));

        metrics.TryGet(
            "Simulation.Update",
            out var metric);

        Assert.Equal(
            MetricType.Timing,
            metric.Type);

        Assert.Equal(
            2.5,
            metric.Value);
    }

    [Fact]
    public void Clear_RemovesAllMetrics()
    {
        var metrics = new MetricCollector();

        metrics.Increment("A");
        metrics.Set("B", 10);

        metrics.Clear();

        Assert.Empty(metrics.Metrics);
    }
}