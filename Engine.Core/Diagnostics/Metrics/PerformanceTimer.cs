using System.Diagnostics;

namespace Engine.Core.Diagnostics.Metrics;

public readonly struct PerformanceTimer
{
    private readonly Stopwatch _stopwatch;

    private PerformanceTimer(
        Stopwatch stopwatch)
    {
        _stopwatch = stopwatch;
    }

    public static PerformanceTimer Start()
    {
        return new PerformanceTimer(
            Stopwatch.StartNew());
    }

    public TimeSpan Elapsed =>
        _stopwatch.Elapsed;
}