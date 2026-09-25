using System.Runtime.InteropServices;

namespace Engine.Memory.Native;

public unsafe sealed class RawMemoryBlock :
    IDisposable
{
    private void* _pointer;
    private bool _disposed;

    public RawMemoryBlock(
        nuint byteLength)
    {
        if (byteLength >
            (nuint)int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteLength),
                "The memory block is too large for a Span<byte>.");
        }

        if (byteLength == 0)
        {
            return;
        }

        _pointer =
            NativeMemory.Alloc(
                byteLength);

        ByteLength =
            byteLength;
    }

    public nuint ByteLength { get; private set; }

    public bool IsAllocated =>
        !_disposed &&
        ByteLength != 0;

    public Span<byte> Span
    {
        get
        {
            EnsureNotDisposed();

            return new Span<byte>(
                _pointer,
                checked((int)ByteLength));
        }
    }

    public void Clear()
    {
        EnsureNotDisposed();

        if (ByteLength == 0)
        {
            return;
        }

        NativeMemory.Clear(
            _pointer,
            ByteLength);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_pointer is not null)
        {
            NativeMemory.Free(
                _pointer);

            _pointer =
                null;
        }

        ByteLength =
            0;

        _disposed =
            true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}