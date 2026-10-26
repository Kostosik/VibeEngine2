using Engine.Core.Diagnostics.Metrics;

namespace Engine.Tests.Core.Diagnostics.Metrics;

public sealed class PerformanceTimerTests
{
    [Fact]
    public void Start_CreatesRunningTimer()
    {
        var timer = PerformanceTimer.Start();

        Thread.Sleep(10);

        Assert.True(
            timer.Elapsed > TimeSpan.Zero);
    }

    [Fact]
    public void DefaultTimerHasZeroElapsedTime()
    {
        var timer = default(PerformanceTimer);

        Assert.Equal(
            TimeSpan.Zero,
            timer.Elapsed);
    }
}