using Engine.Core.Time;

namespace Engine.Tests.Time;

public sealed class SimulationRateTests
{
    [Fact]
    public void TickDurationIsCalculatedCorrectly()
    {
        var rate =
            new SimulationRate(20);

        Assert.Equal(
            50,
            rate.TickDuration.TotalMilliseconds);
    }

    [Fact]
    public void ZeroTicksPerSecondThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                new SimulationRate(0);
            });
    }

    [Fact]
    public void NegativeTicksPerSecondThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                new SimulationRate(-1);
            });
    }
}