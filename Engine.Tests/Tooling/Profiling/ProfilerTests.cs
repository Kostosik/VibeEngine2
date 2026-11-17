
using Engine.Tooling.Profiling;

namespace Engine.Tests.Tooling.Profiling;

public sealed class ProfilerTests
{
    [Fact]
    public void Profiler_IsDisabledByDefault()
    {
        var profiler = new Profiler();

        Assert.False(profiler.IsCapturing);

        using var scope =
            profiler.BeginScope("Test.Scope");

        profiler.RecordFrame();

        Assert.Empty(profiler.Metrics);
        Assert.Equal(0, profiler.FramesPerSecond);
        Assert.Equal(TimeSpan.Zero, profiler.LastFrameTime);
    }

    [Fact]
    public void BeginScope_WhenDisabled_ReturnsSharedNoOpScope()
    {
        var profiler = new Profiler();

        var first = profiler.BeginScope("First");
        var second = profiler.BeginScope("Second");

        Assert.Same(first, second);

        first.Dispose();
        second.Dispose();

        Assert.Empty(profiler.Metrics);
    }

    [Fact]
    public void StartCapture_EnablesScopeMeasurements()
    {
        var profiler = new Profiler();

        profiler.StartCapture();

        Assert.True(profiler.IsCapturing);

        using (profiler.BeginScope("Test.Scope"))
        {
        }

        Assert.True(
            profiler.TryGetMetric(
                "Test.Scope.Calls",
                out var calls));

        Assert.Equal(1, calls.Value);

        Assert.True(
            profiler.TryGetMetric(
                "Test.Scope",
                out var timing));

        Assert.Equal(
            Engine.Core.Diagnostics.Metrics.MetricType.Timing,
            timing.Type);
    }

    [Fact]
    public void StopCapture_DisablesMeasurementsAndPreservesMetrics()
    {
        var profiler = new Profiler();

        profiler.StartCapture();

        using (profiler.BeginScope("BeforeStop"))
        {
        }

        profiler.StopCapture();

        Assert.False(profiler.IsCapturing);

        Assert.True(
            profiler.TryGetMetric(
                "BeforeStop.Calls",
                out var before));

        using (profiler.BeginScope("AfterStop"))
        {
        }

        profiler.RecordFrame();

        Assert.True(
            profiler.TryGetMetric(
                "BeforeStop.Calls",
                out var after));

        Assert.Equal(before.Value, after.Value);

        Assert.False(
            profiler.TryGetMetric(
                "AfterStop.Calls",
                out _));
    }

    [Fact]
    public void StartCapture_ClearsPreviousSession()
    {
        var profiler = new Profiler();

        profiler.StartCapture();

        using (profiler.BeginScope("Previous"))
        {
        }

        profiler.StopCapture();
        profiler.StartCapture();

        Assert.Empty(profiler.Metrics);
    }

    [Fact]
    public void StartCapture_CanBeCalledMultipleTimes()
    {
        var profiler = new Profiler();

        profiler.StartCapture();
        profiler.StartCapture();

        Assert.True(profiler.IsCapturing);
    }

    [Fact]
    public void StopCapture_CanBeCalledMultipleTimes()
    {
        var profiler = new Profiler();

        profiler.StartCapture();

        profiler.StopCapture();
        profiler.StopCapture();

        Assert.False(profiler.IsCapturing);
    }
}
