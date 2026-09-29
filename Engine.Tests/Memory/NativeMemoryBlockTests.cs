using Engine.Memory.Native;

namespace Engine.Tests.Memory;

public sealed class NativeMemoryBlockTests
{
    [Fact]
    public void Constructor_SetsLength()
    {
        using var block =
            new NativeMemoryBlock<int>(
                8);

        Assert.Equal(
            8,
            block.Length);

        Assert.Equal(
            8,
            block.Span.Length);
    }

    [Fact]
    public void Span_ReadsAndWritesTypedMemory()
    {
        using var block =
            new NativeMemoryBlock<int>(
                4);

        block.Span[1] =
            42;

        block[2] =
            17;

        Assert.Equal(
            42,
            block[1]);

        Assert.Equal(
            17,
            block.Span[2]);
    }

    [Fact]
    public void Clear_ResetsTypedMemory()
    {
        using var block =
            new NativeMemoryBlock<int>(
                4);

        block.Span.Fill(
            42);

        block.Clear();

        Assert.All(
            block.Span.ToArray(),
            value =>
                Assert.Equal(
                    0,
                    value));
    }

    [Fact]
    public void ZeroLengthBlock_IsEmpty()
    {
        using var block =
            new NativeMemoryBlock<int>(
                0);

        Assert.Equal(
            0,
            block.Length);

        Assert.Equal(0,
            block.Span.Length);
    }

    [Fact]
    public void DisposedBlock_RejectsSpanAndClear()
    {
        var block =
            new NativeMemoryBlock<int>(
                4);

        block.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = block.Span;
            });

        Assert.Throws<ObjectDisposedException>(
            () =>
                block.Clear());
    }

    [Fact]
    public void Indexer_RejectsOutOfRange()
    {
        using var block =
            new NativeMemoryBlock<int>(
                4);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                _ = block[4];
            });
    }
}