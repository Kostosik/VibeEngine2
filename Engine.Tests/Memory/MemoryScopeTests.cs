using Engine.Memory.Scopes;

namespace Engine.Tests.Memory;

public sealed class MemoryScopeTests
{
    [Fact]
    public void AllocateBytes_ReturnsRequestedLength()
    {
        using var scope =
            new MemoryScope(
                64);

        var memory =
            scope.AllocateBytes(
                16);

        Assert.Equal(
            16,
            memory.Length);

        Assert.Equal(
            16,
            scope.Used);
    }

    [Fact]
    public unsafe void AllocateBytes_RespectsAlignment()
    {
        using var scope =
            new MemoryScope(
                64);

        scope.AllocateBytes(
            1);

        var aligned =
            scope.AllocateBytes(
                8,
                8);

        fixed (byte* pointer = aligned)
        {
            Assert.Equal(
                (nuint)0,
                (nuint)pointer % 8);
        }
    }

    [Fact]
    public void Reset_ReusesExistingLargerBlockInsteadOfAllocating()
    {
        using var scope =
            new MemoryScope(
                64);

        scope.AllocateBytes(64);
        scope.AllocateBytes(128);
        scope.AllocateBytes(256);

        var capacityBefore =
            scope.Capacity;

        scope.Reset();

        scope.AllocateBytes(
            200);

        Assert.Equal(
            capacityBefore,
            scope.Capacity);

        Assert.Equal(
            392,
            scope.Used);
    }

    [Fact]
    public void AllocateBytes_GrowsWhenCurrentBlockDoesNotFit()
    {
        using var scope =
            new MemoryScope(
                8);

        scope.AllocateBytes(
            8);

        var values =
            scope.AllocateBytes(
                16);

        Assert.Equal(
            16,
            values.Length);

        Assert.True(
            scope.Capacity >= 24);

        Assert.True(
            scope.Used >= 24);
    }

    [Fact]
    public void AllocateTyped_ReturnsTypedSpan()
    {
        using var scope =
            new MemoryScope(
                64);

        var values =
            scope.Allocate<int>(
                4);

        values[0] =
            10;

        values[3] =
            40;

        Assert.Equal(
            4,
            values.Length);

        Assert.Equal(
            10,
            values[0]);

        Assert.Equal(
            40,
            values[3]);
    }

    [Fact]
    public void Reset_ReusesScopeFromBeginning()
    {
        using var scope =
            new MemoryScope(
                64);

        scope.AllocateBytes(
            32);

        var capacityBefore =
            scope.Capacity;

        scope.Reset();

        Assert.Equal(
            0,
            scope.Used);

        Assert.Equal(
            capacityBefore,
            scope.Capacity);

        scope.AllocateBytes(
            8);

        Assert.Equal(
            8,
            scope.Used);
    }

    [Fact]
    public void AllocateBytes_ZeroLengthReturnsEmptySpan()
    {
        using var scope =
            new MemoryScope(
                64);

        var values =
            scope.AllocateBytes(
                0);

        Assert.True(
            values.IsEmpty);

        Assert.Equal(
            0,
            scope.Used);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    public void AllocateBytes_RejectsNonPowerOfTwoAlignment(
        int alignment)
    {
        using var scope =
            new MemoryScope(
                64);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                scope.AllocateBytes(
                    1,
                    alignment));
    }

    [Fact]
    public void DisposedScope_RejectsAllocation()
    {
        var scope =
            new MemoryScope();

        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
                scope.AllocateBytes(1));
    }
}