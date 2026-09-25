using Engine.Memory.Diagnostics;
using System.Runtime.CompilerServices;
using System.Buffers;

namespace Engine.Memory.Pools;

public sealed class PooledBuffer<T> :
    IDisposable
{
    private T[]? _buffer;
    private readonly bool _pooled;
    private readonly MemoryStatisticsCollector? _statistics;

    internal PooledBuffer(
        T[] buffer,
        int length,
        bool pooled,
        MemoryStatisticsCollector? statistics)
    {
        ArgumentNullException.ThrowIfNull(
            buffer);

        if (length < 0 ||
            length > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        _buffer =
            buffer;

        Length =
            length;

        _pooled =
            pooled;

        _statistics =
            statistics;

        if (_pooled)
        {
            _statistics?.RecordRent(
                checked(
                    buffer.Length *
                    Unsafe.SizeOf<T>()));
        }
    }

    public int Length { get; }

    public int Capacity =>
        _buffer?.Length ?? 0;

    public Span<T> Span
    {
        get
        {
            EnsureNotDisposed();

            return _buffer!
                .AsSpan(
                    0,
                    Length);
        }
    }

    public Memory<T> Memory
    {
        get
        {
            EnsureNotDisposed();

            return _buffer!
                .AsMemory(
                    0,
                    Length);
        }
    }

    public ref T this[int index]
    {
        get
        {
            EnsureNotDisposed();

            if ((uint)index >=
                (uint)Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            return ref _buffer![index];
        }
    }

    public void Dispose()
    {
        var buffer =
            Interlocked.Exchange(
                ref _buffer,
                null);

        if (buffer is null ||
            !_pooled)
        {
            return;
        }

        Array.Clear(
            buffer,
            0,
            buffer.Length);

        ArrayPool<T>.Shared.Return(
            buffer);

        _statistics?.RecordReturn(
            checked(
                buffer.Length *
                Unsafe.SizeOf<T>()));
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _buffer is null,
            this);
    }
}