using Engine.Memory.Blocks;

namespace Engine.Memory.Buffers;

public sealed class ResizableBuffer<T> :
    IDisposable
{
    private MemoryBlock<T>? _block;
    private int _length;

    public ResizableBuffer(
        int capacity = 16)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity));
        }

        _block =
            new MemoryBlock<T>(
                capacity);
    }

    public int Length =>
        _length;

    public int Capacity =>
        _block?.Length ?? 0;

    public Span<T> Span
    {
        get
        {
            EnsureNotDisposed();

            return _block!
                .Span
                .Slice(
                    0,
                    _length);
        }
    }

    public Memory<T> Memory
    {
        get
        {
            EnsureNotDisposed();

            return _block!
                .Memory
                .Slice(
                    0,
                    _length);
        }
    }

    public ref T this[int index]
    {
        get
        {
            EnsureNotDisposed();

            if ((uint)index >=
                (uint)_length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            return ref _block![index];
        }
    }

    public void Add(
        T value)
    {
        EnsureNotDisposed();

        EnsureCapacity(checked(
            _length + 1));

        _block![_length] =
            value;

        _length++;
    }

    public void AddRange(
        ReadOnlySpan<T> values)
    {
        EnsureNotDisposed();

        if (values.Length == 0)
        {
            return;
        }

        EnsureCapacity(
            checked(
                _length +
                values.Length));

        values.CopyTo(
            _block!
                .Span
                .Slice(
                    _length,
                    values.Length));

        _length +=
            values.Length;
    }

    public void Clear()
    {
        EnsureNotDisposed();

        _length = 0;
    }

    public void EnsureCapacity(
        int capacity)
    {
        EnsureNotDisposed();

        if (capacity <= Capacity)
        {
            return;
        }

        var newCapacity =
            CalculateCapacity(
                Capacity,
                capacity);

        var newBlock =
            new MemoryBlock<T>(
                newCapacity);

        _block!
            .Span
            .Slice(
                0,
                _length)
            .CopyTo(
                newBlock.Span);

        _block.Dispose();

        _block =
            newBlock;
    }

    public void Dispose()
    {
        var block =
            Interlocked.Exchange(
                ref _block,
                null);

        if (block is null)
        {
            return;
        }

        block.Dispose();

        _length = 0;
    }

    private static int CalculateCapacity(
        int currentCapacity,
        int requiredCapacity)
    {
        var capacity =
            currentCapacity == 0
                ? 4
                : currentCapacity;

        while (capacity < requiredCapacity)
        {
            var nextCapacity =
                capacity <=
                int.MaxValue / 2
                    ? capacity * 2
                    : int.MaxValue;

            if (nextCapacity == capacity)
            {
                throw new OutOfMemoryException(
                    "Unable to grow memory buffer.");
            }

            capacity =
                nextCapacity;
        }

        return capacity;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _block is null,
            this);
    }
}