using Engine.Memory.Buffers;

namespace Engine.Tests.Memory;

public sealed class ResizableBufferTests
{
    [Fact]
    public void Add_IncreasesLength()
    {
        using var buffer =
            new ResizableBuffer<int>(
                2);

        buffer.Add(10);
        buffer.Add(20);

        Assert.Equal(
            2,
            buffer.Length);

        Assert.Equal(
            10,
            buffer[0]);

        Assert.Equal(
            20,
            buffer[1]);
    }

    [Fact]
    public void Add_GrowsAndPreservesExistingData()
    {
        using var buffer =
            new ResizableBuffer<int>(
                1);

        buffer.Add(10);
        var oldCapacity =
            buffer.Capacity;

        buffer.Add(20);
        buffer.Add(30);

        Assert.True(
            buffer.Capacity >
            oldCapacity);

        Assert.Equal(
            3,
            buffer.Length);

        Assert.Equal(
            10,
            buffer[0]);

        Assert.Equal(
            20,
            buffer[1]);

        Assert.Equal(
            30,
            buffer[2]);
    }

    [Fact]
    public void AddRange_AddsAllValues()
    {
        using var buffer =
            new ResizableBuffer<int>();

        buffer.AddRange(
            new[]
            {
                1,
                2,
                3,
                4
            });

        Assert.Equal(
            4,
            buffer.Length);

        Assert.Equal(
            new[]
            {
                1,
                2,
                3,
                4
            },
            buffer.Span.ToArray());
    }

    [Fact]
    public void Clear_ResetsLengthWithoutChangingCapacity()
    {
        using var buffer =
            new ResizableBuffer<int>();

        buffer.Add(1);
        buffer.Add(2);

        var capacity =
            buffer.Capacity;

        buffer.Clear();

        Assert.Equal(
            0,
            buffer.Length);

        Assert.Equal(
            capacity,
            buffer.Capacity);
    }

    [Fact]
    public void EnsureCapacity_DoesNotShrink()
    {
        using var buffer =
            new ResizableBuffer<int>(
                8);

        buffer.EnsureCapacity(4);

        Assert.Equal(
            8,
            buffer.Capacity);
    }

    [Fact]
    public void DisposedBuffer_RejectsAccess()
    {
        var buffer =
            new ResizableBuffer<int>();

        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = buffer.Span;
            });
    }
}