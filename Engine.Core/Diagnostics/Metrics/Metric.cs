namespace Engine.Core.Diagnostics.Metrics;

public readonly record struct Metric(
    string Name,
    MetricType Type,
    double Value);