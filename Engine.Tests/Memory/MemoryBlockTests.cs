using Engine.Memory.Blocks;

namespace Engine.Tests.Memory;

public sealed class MemoryBlockTests
{
    [Fact]
    public void Constructor_SetsLength()
    {
        using var block =
            new MemoryBlock<int>(
                32);

        Assert.Equal(
            32,
            block.Length);
    }

    [Fact]
    public void Indexer_ReadsAndWrites()
    {
        using var block =
            new MemoryBlock<int>(
                4);

        block[2] =
            42;

        Assert.Equal(
            42,
            block[2]);
    }

    [Fact]
    public void Span_ExposesEntireBlock()
    {
        using var block =
            new MemoryBlock<int>(
                4);

        Assert.Equal(
            4,
            block.Span.Length);

        block.Span[1] =
            17;

        Assert.Equal(
            17,
            block[1]);
    }

    [Fact]
    public void Indexer_ThrowsWhenOutsideBounds()
    {
        using var block =
            new MemoryBlock<int>(
                4);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                _ = block[4];
            });
    }

    [Fact]
    public void DisposedBlock_RejectsAccess()
    {
        var block =
            new MemoryBlock<int>(
                4);

        block.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = block.Span;
            });
    }
}