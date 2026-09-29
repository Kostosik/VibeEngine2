using Engine.Memory.Arenas;

namespace Engine.Tests.Memory;

public sealed class MemoryArenaTests
{
    [Fact]
    public void Allocate_UsesCurrentBufferUntilCapacityIsReached()
    {
        using var arena =
            new MemoryArena<int>(
                4);

        var first =
            arena.Allocate(2);

        first[0] =
            10;

        first[1] =
            20;

        var second =
            arena.Allocate(2);

        second[0] =
            30;

        second[1] =
            40;

        Assert.Equal(
            4,
            arena.AllocatedCount);

        Assert.Equal(
            10,
            first[0]);

        Assert.Equal(
            40,
            second[1]);
    }

    [Fact]
    public void Allocate_GrowsToAnotherBufferWhenCurrentBufferIsFull()
    {
        using var arena =
            new MemoryArena<int>(
                4);

        arena.Allocate(4);

        var second =
            arena.Allocate(1);

        second[0] =
            42;

        Assert.Equal(
            8,
            arena.AllocatedCount);

        Assert.Equal(
            42,
            second[0]);
    }

    [Fact]
    public void Allocate_LargeAllocationGetsDedicatedBuffer()
    {
        using var arena =
            new MemoryArena<int>(
                4);

        var values =
            arena.Allocate(5);

        values[3] =
            42;

        Assert.Equal(
            5,
            arena.AllocatedCount);

        Assert.Equal(
            42,
            values[3]);
    }

    [Fact]
    public void Reset_ReleasesAllBuffersAndAllowsReuse()
    {
        using var arena =
            new MemoryArena<int>(
                4);

        arena.Allocate(8);

        Assert.True(
            arena.AllocatedCount > 0);

        arena.Reset();

        Assert.Equal(
            0,
            arena.AllocatedCount);

        var values =
            arena.Allocate(2);

        values[0] =
            17;

        Assert.Equal(
            4,
            arena.AllocatedCount);

        Assert.Equal(
            17,
            values[0]);
    }

    [Fact]
    public void Allocate_ZeroLengthReturnsEmptySpan()
    {
        using var arena =
            new MemoryArena<int>(
                4);

        var values =
            arena.Allocate(0);

        Assert.True(
            values.IsEmpty);

        Assert.Equal(
            0,
            arena.AllocatedCount);
    }

    [Fact]
    public void DisposedArena_RejectsAllocation()
    {
        var arena =
            new MemoryArena<int>();

        arena.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
                arena.Allocate(1));
    }
}