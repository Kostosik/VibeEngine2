using Engine.Memory.Diagnostics;

namespace Engine.Tests.Memory;

public sealed class MemoryStatisticsCollectorTests
{
    [Fact]
    public void RecordRentAndReturn_UpdatesStatistics()
    {
        var collector =
            new MemoryStatisticsCollector();

        collector.RecordRent(
            128);

        collector.RecordRent(
            64);

        collector.RecordReturn(
            128);

        var statistics =
            collector.Statistics;

        Assert.Equal(
            192,
            statistics.TotalRentedBytes);

        Assert.Equal(
            128,
            statistics.TotalReturnedBytes);

        Assert.Equal(
            64,
            statistics.ActiveRentedBytes);

        Assert.Equal(
            2,
            statistics.RentCount);

        Assert.Equal(
            1,
            statistics.ReturnCount);
    }

    [Fact]
    public void Reset_ClearsStatistics()
    {
        var collector =
            new MemoryStatisticsCollector();

        collector.RecordRent(
            128);

        collector.RecordReturn(
            64);

        collector.Reset();

        Assert.Equal(
            MemoryStatistics.Empty,
            collector.Statistics);
    }

    [Fact]
    public void NegativeBytes_AreRejected()
    {
        var collector =
            new MemoryStatisticsCollector();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                collector.RecordRent(-1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                collector.RecordReturn(-1));
    }
}