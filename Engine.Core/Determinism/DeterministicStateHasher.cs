using Engine.Core.Math;
using System.Buffers.Binary;
using System.Text;

namespace Engine.Core.Determinism;

public ref struct DeterministicStateHasher
{
    private const ulong OffsetBasis =
        14695981039346656037UL;

    private const ulong Prime =
        1099511628211UL;

    private ulong _value;

    private DeterministicStateHasher(
        ulong value)
    {
        _value = value;
    }

    public static DeterministicStateHasher Create()
    {
        return new(
            OffsetBasis);
    }

    public void AddByte(
        byte value)
    {
        _value ^=
            value;

        _value *=
            Prime;
    }

    public void AddBool(
        bool value)
    {
        AddByte(
            value ? (byte)1 : (byte)0);
    }

    public void AddInt32(
        int value)
    {
        Span<byte> bytes =
            stackalloc byte[sizeof(int)];

        BinaryPrimitives.WriteInt32LittleEndian(
            bytes,
            value);

        AddBytes(bytes);
    }

    public void AddUInt32(
        uint value)
    {
        Span<byte> bytes =
            stackalloc byte[sizeof(uint)];

        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes,
            value);

        AddBytes(bytes);
    }

    public void AddInt64(
        long value)
    {
        Span<byte> bytes =
            stackalloc byte[sizeof(long)];

        BinaryPrimitives.WriteInt64LittleEndian(
            bytes,
            value);

        AddBytes(bytes);
    }

    public void AddUInt64(
        ulong value)
    {
        Span<byte> bytes =
            stackalloc byte[sizeof(ulong)];

        BinaryPrimitives.WriteUInt64LittleEndian(
            bytes,
            value);

        AddBytes(bytes);
    }

    public void AddBytes(
        scoped ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            AddByte(value);
        }
    }

    public void AddFixed32(
        Fixed32 value)
    {
        AddInt32(
            value.RawValue);
    }

    public void AddFixedVector2(
        FixedVector2 value)
    {
        AddFixed32(value.X);
        AddFixed32(value.Y);
    }

    public void AddString(
    string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var bytes =
            Encoding.UTF8.GetBytes(value);

        AddUInt32(
            (uint)bytes.Length);

        AddBytes(bytes);
    }

    public DeterministicStateHash GetHash()
    {
        return new(
            _value);
    }
}