namespace Engine.Memory.Blocks;

public sealed class MemoryBlock<T> :
    IDisposable
{
    private T[]? _buffer;

    public MemoryBlock(
        int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        _buffer =
            new T[length];
    }

    public int Length =>
        _buffer?.Length ?? 0;

    public Span<T> Span
    {
        get
        {
            EnsureNotDisposed();

            return _buffer!;
        }
    }

    public Memory<T> Memory
    {
        get
        {
            EnsureNotDisposed();

            return _buffer!;
        }
    }

    public ref T this[int index]
    {
        get
        {
            EnsureNotDisposed();

            if ((uint)index >=
                (uint)_buffer!.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            return ref _buffer[index];
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(
            ref _buffer,
            null);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _buffer is null,
            this);
    }
}