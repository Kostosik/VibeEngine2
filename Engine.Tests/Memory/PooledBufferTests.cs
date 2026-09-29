using Engine.Memory.Diagnostics;
using Engine.Memory.Pools;

namespace Engine.Tests.Memory;

public sealed class PooledBufferTests
{
    [Fact]
    public void Rent_TracksStatistics()
    {
        var statistics =
            new MemoryStatisticsCollector();

        var pool =
            new MemoryPool<int>(
                statistics);

        using var buffer =
            pool.Rent(4);

        var afterRent =
            statistics.Statistics;

        Assert.Equal(
            1,
            afterRent.RentCount);

        Assert.Equal(
            0,
            afterRent.ReturnCount);

        Assert.True(
            afterRent.ActiveRentedBytes > 0);

        Assert.Equal(
            afterRent.TotalRentedBytes,
            afterRent.ActiveRentedBytes);
    }

    [Fact]
    public void Dispose_ReturnsBufferExactlyOnce()
    {
        var statistics =
            new MemoryStatisticsCollector();

        var pool =
            new MemoryPool<int>(
                statistics);

        var buffer =
            pool.Rent(4);

        buffer.Dispose();
        buffer.Dispose();

        var stats =
            statistics.Statistics;

        Assert.Equal(
            1,
            stats.RentCount);

        Assert.Equal(
            1,
            stats.ReturnCount);

        Assert.Equal(
            0,
            stats.ActiveRentedBytes);
    }

    [Fact]
    public void DisposedBuffer_RejectsAllAccess()
    {
        var pool =
            new MemoryPool<int>();

        var buffer =
            pool.Rent(4);

        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = buffer.Span;
            });

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = buffer.Memory;
            });

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = buffer[0];
            });
    }

    [Fact]
    public void Rent_ExposesRequestedLogicalLength()
    {
        var pool =
            new MemoryPool<int>();

        using var buffer =
            pool.Rent(5);

        Assert.Equal(
            5,
            buffer.Length);

        Assert.True(
            buffer.Capacity >=
            buffer.Length);

        Assert.Equal(
            5,
            buffer.Span.Length);

        Assert.Equal(
            5,
            buffer.Memory.Length);
    }

    [Fact]
    public void ZeroLengthRent_IsNotTrackedAsPooledAllocation()
    {
        var statistics =
            new MemoryStatisticsCollector();

        var pool =
            new MemoryPool<int>(
                statistics);

        using var buffer =
            pool.Rent(0);

        var stats =
            statistics.Statistics;

        Assert.Equal(
            0,
            stats.RentCount);

        Assert.Equal(
            0,
            stats.ReturnCount);

        Assert.Equal(
            0,
            stats.ActiveRentedBytes);

        Assert.Equal(
            0,
            buffer.Length);

        Assert.Equal(
            0,
            buffer.Span.Length);
    }
}