using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine.Memory.Native;

public unsafe sealed class NativeMemoryBlock<T> :
    IDisposable
    where T : unmanaged
{
    private readonly RawMemoryBlock _memory;

    public NativeMemoryBlock(
        int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        var byteLength =
            checked(
                (nuint)length *
                (nuint)Unsafe.SizeOf<T>());

        _memory =
            new RawMemoryBlock(
                byteLength);

        Length =
            length;
    }

    public int Length { get; }

    public Span<T> Span
    {
        get
        {
            return MemoryMarshal.Cast<byte, T>(
                _memory.Span);
        }
    }

    public ref T this[int index]
    {
        get
        {
            if ((uint)index >=
                (uint)Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            return ref Span[index];
        }
    }

    public void Clear()
    {
        _memory.Clear();
    }

    public void Dispose()
    {
        _memory.Dispose();
    }
}