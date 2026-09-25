using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Memory.Native;

namespace Engine.Memory.Scopes;

public unsafe sealed class MemoryScope :
    IDisposable
{
    private readonly List<RawMemoryBlock> _blocks = new();

    private int _currentBlockIndex;
    private int _currentOffset;

    private bool _disposed;

    public MemoryScope(
        int initialCapacity = 64 * 1024)
    {
        if (initialCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCapacity));
        }

        AddBlock(
            initialCapacity);
    }

    public int Capacity
    {
        get
        {
            EnsureNotDisposed();

            var capacity = 0;

            foreach (var block in _blocks)
            {
                capacity =
                    checked(
                        capacity +
                        checked(
                            (int)block.ByteLength));
            }

            return capacity;
        }
    }

    public int Used
    {
        get
        {
            EnsureNotDisposed();

            var used = 0;

            for (var i = 0;
                 i < _currentBlockIndex;
                 i++)
            {
                used =
                    checked(
                        used +
                        checked(
                            (int)_blocks[i].ByteLength));
            }

            return checked(
                used +
                _currentOffset);
        }
    }

    public Span<byte> AllocateBytes(
        int byteCount,
        int alignment = 1)
    {
        EnsureNotDisposed();

        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteCount));
        }

        ValidateAlignment(
            alignment);

        if (byteCount == 0)
        {
            return Span<byte>.Empty;
        }

        var block =
            _blocks[_currentBlockIndex];

        var alignedOffset =
            AlignUp(
                _currentOffset,
                alignment);

        if (alignedOffset >
            (int)block.ByteLength ||
            byteCount >
            (int)block.ByteLength -
            alignedOffset)
        {
            MoveToBlock(
                byteCount,
                alignment);

            block =
                _blocks[_currentBlockIndex];

            alignedOffset =
                AlignUp(
                    _currentOffset,
                    alignment);
        }

        var result =
            block.Span.Slice(
                alignedOffset,
                byteCount);

        _currentOffset =
            checked(
                alignedOffset +
                byteCount);

        return result;
    }

    public Span<T> Allocate<T>(
        int count)
        where T : unmanaged
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count));
        }

        if (count == 0)
        {
            return Span<T>.Empty;
        }

        var byteCount =
            checked(
                count *
                Unsafe.SizeOf<T>());

        var bytes =
            AllocateBytes(
                byteCount,
                IntPtr.Size);

        return MemoryMarshal.Cast<byte, T>(
            bytes);
    }

    public ref T Allocate<T>()
        where T : unmanaged
    {
        return ref Allocate<T>(1)[0];
    }

    public void Reset()
    {
        EnsureNotDisposed();

        _currentBlockIndex = 0;
        _currentOffset = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var block in _blocks)
        {
            block.Dispose();
        }

        _blocks.Clear();

        _currentBlockIndex = 0;
        _currentOffset = 0;

        _disposed = true;
    }

    private void MoveToBlock(
        int byteCount,
        int alignment)
    {
        if (_currentBlockIndex + 1 <
            _blocks.Count)
        {
            _currentBlockIndex++;
            _currentOffset = 0;

            var existingBlock =
                _blocks[_currentBlockIndex];

            var alignedOffset =
                AlignUp(
                    0,
                    alignment);

            if (byteCount <=
                (int)existingBlock.ByteLength -
                alignedOffset)
            {
                return;
            }
        }

        var requiredCapacity =
            checked(
                byteCount +
                alignment -
                1);

        var currentCapacity =
            checked(
                (int)_blocks[_currentBlockIndex]
                    .ByteLength);

        var doubledCapacity =
            currentCapacity >
            int.MaxValue / 2
                ? int.MaxValue
                : currentCapacity * 2;

        var newCapacity =
            Math.Max(
                doubledCapacity,
                requiredCapacity);

        if (newCapacity <= 0)
        {
            throw new OutOfMemoryException(
                "Unable to grow memory scope.");
        }

        AddBlock(
            newCapacity);

        _currentBlockIndex =
            _blocks.Count - 1;

        _currentOffset = 0;
    }

    private void AddBlock(
        int capacity)
    {
        _blocks.Add(
            new RawMemoryBlock(
                checked(
                    (nuint)capacity)));
    }

    private static int AlignUp(
        int value,
        int alignment)
    {
        var remainder =
            value %
            alignment;

        if (remainder == 0)
        {
            return value;
        }

        return checked(
            value +
            alignment -
            remainder);
    }

    private static void ValidateAlignment(
        int alignment)
    {
        if (alignment < 1 ||
            (alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alignment),
                "Alignment must be a positive power of two.");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}