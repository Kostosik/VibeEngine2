using Engine.Memory.Native;

namespace Engine.Tests.Memory;

public sealed class RawMemoryBlockTests
{
    [Fact]
    public void Constructor_AllocatesRequestedSize()
    {
        using var block =
            new RawMemoryBlock(
                128);

        Assert.Equal(
            (nuint)128,
            block.ByteLength);

        Assert.True(
            block.IsAllocated);

        Assert.Equal(
            128,
            block.Span.Length);
    }

    [Fact]
    public void Span_ReadsAndWritesNativeMemory()
    {
        using var block =
            new RawMemoryBlock(
                16);

        block.Span[5] =
            123;

        Assert.Equal(
            123,
            block.Span[5]);
    }

    [Fact]
    public void Clear_ResetsMemory()
    {
        using var block =
            new RawMemoryBlock(
                16);

        block.Span.Fill(
            255);

        block.Clear();

        Assert.All(
            block.Span.ToArray(),
            value =>
                Assert.Equal(
                    0,
                    value));
    }

    [Fact]
    public void ZeroLengthBlock_IsNotAllocated()
    {
        using var block =
            new RawMemoryBlock(
                0);

        Assert.Equal(
            (nuint)0,
            block.ByteLength);

        Assert.False(
            block.IsAllocated);

        Assert.Equal(
            0,
            block.Span.Length);
    }

    [Fact]
    public void Dispose_MakesBlockUnavailable()
    {
        var block =
            new RawMemoryBlock(
                16);

        block.Dispose();

        Assert.False(
            block.IsAllocated);

        Assert.Equal(
            (nuint)0,
            block.ByteLength);

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = block.Span;
            });
    }

    [Fact]
    public void TooLargeBlock_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new RawMemoryBlock(
                    (nuint)int.MaxValue + 1);
            });
    }
}