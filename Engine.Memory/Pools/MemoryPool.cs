using Engine.Memory.Diagnostics;
using System.Buffers;

namespace Engine.Memory.Pools;

public sealed class MemoryPool<T>
{
    public MemoryPool()
        : this(
            new MemoryStatisticsCollector())
    {
    }

    public MemoryPool(
        MemoryStatisticsCollector statistics)
    {
        ArgumentNullException.ThrowIfNull(
            statistics);

        Statistics =
            statistics;
    }

    public MemoryStatisticsCollector Statistics { get; }

    public PooledBuffer<T> Rent(
        int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        if (length == 0)
        {
            return new PooledBuffer<T>(
                Array.Empty<T>(),
                0,
                false,
                null);
        }

        var buffer =
            ArrayPool<T>.Shared.Rent(
                length);

        return new PooledBuffer<T>(
            buffer,
            length,
            true,
            Statistics);
    }
}