using Engine.Memory.Pools;

namespace Engine.Memory.Collections;

public sealed class PooledList<T> :
    IDisposable
{
    private readonly MemoryPool<T> _pool;
    private PooledBuffer<T>? _buffer;
    private int _count;
    private bool _disposed;

    public PooledList(
        int capacity = 16)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity));
        }

        _pool =
            new MemoryPool<T>();

        if (capacity > 0)
        {
            _buffer =
                _pool.Rent(
                    capacity);
        }
    }

    public int Count =>
        _count;

    public int Capacity =>
        _buffer?.Capacity ?? 0;

    public ref T this[int index]
    {
        get
        {
            EnsureNotDisposed();
            ValidateIndex(index);

            return ref _buffer!
                .Span[index];
        }
    }

    public void Add(
        T value)
    {
        EnsureNotDisposed();

        EnsureCapacity(checked(
            _count + 1));

        _buffer!.Span[_count] =
            value;

        _count++;
    }

    public void AddRange(
        ReadOnlySpan<T> values)
    {
        EnsureNotDisposed();

        if (values.IsEmpty)
        {
            return;
        }

        EnsureCapacity(
            checked(
                _count +
                values.Length));

        values.CopyTo(
            _buffer!
                .Span
                .Slice(
                    _count,
                    values.Length));

        _count +=
            values.Length;
    }

    public void RemoveAt(
        int index)
    {
        EnsureNotDisposed();
        ValidateIndex(index);

        var lastIndex =
            _count - 1;

        if (index != lastIndex)
        {
            _buffer!.Span[index] =
                _buffer.Span[lastIndex];
        }

        _count--;
    }

    public void Clear()
    {
        EnsureNotDisposed();

        _count = 0;
    }

    public Span<T> AsSpan()
    {
        EnsureNotDisposed();

        return _buffer is null
            ? Span<T>.Empty
            : _buffer.Span.Slice(
                0,
                _count);
    }

    public ReadOnlySpan<T> AsReadOnlySpan()
    {
        EnsureNotDisposed();

        return AsSpan();
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
            Capacity == 0
                ? 16
                : Capacity;

        while (newCapacity < capacity)
        {
            newCapacity =
                checked(newCapacity * 2);
        }

        var newBuffer =
            _pool.Rent(
                newCapacity);

        if (_buffer is not null &&
            _count > 0)
        {
            _buffer
                .Span
                .Slice(
                    0,
                    _count)
                .CopyTo(
                    newBuffer.Span);
        }

        _buffer?.Dispose();

        _buffer =
            newBuffer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _buffer?.Dispose();
        _buffer = null;
        _count = 0;
        _disposed = true;
    }

    private void ValidateIndex(
        int index)
    {
        if ((uint)index >=
            (uint)_count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}