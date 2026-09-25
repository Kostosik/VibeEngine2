using Engine.Memory.Diagnostics;
using Engine.Memory.Pools;

namespace Engine.Memory.Arenas;

public sealed class MemoryArena<T> :
    IDisposable
{
    private readonly MemoryPool<T> _pool;
    private readonly List<PooledBuffer<T>> _buffers = new();

    private PooledBuffer<T>? _current;
    private int _position;
    private bool _disposed;

    public MemoryArena(
        int bufferCapacity = 1024)
    {
        if (bufferCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bufferCapacity));
        }

        BufferCapacity =
            bufferCapacity;

        _pool =
            new MemoryPool<T>();
    }

    public int BufferCapacity { get; }

    public int AllocatedCount =>
        _buffers.Sum(
            buffer => buffer.Length);

    public MemoryStatistics Statistics =>
        _pool.Statistics.Statistics;

    public Span<T> Allocate(
        int length)
    {
        EnsureNotDisposed();

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        if (length == 0)
        {
            return Span<T>.Empty;
        }

        if (length > BufferCapacity)
        {
            var largeBuffer =
                _pool.Rent(length);

            _buffers.Add(
                largeBuffer);

            return largeBuffer.Span;
        }

        if (_current is null ||
            _position + length > _current.Length)
        {
            _current =
                _pool.Rent(
                    BufferCapacity);

            _buffers.Add(
                _current);

            _position = 0;
        }

        var result =
            _current.Span.Slice(
                _position,
                length);

        _position +=
            length;

        return result;
    }

    public void Reset()
    {
        EnsureNotDisposed();

        foreach (var buffer in _buffers)
        {
            buffer.Dispose();
        }

        _buffers.Clear();

        _current = null;
        _position = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Reset();

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}