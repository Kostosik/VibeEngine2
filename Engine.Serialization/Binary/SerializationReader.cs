using System.Buffers.Binary;
using System.Text;

namespace Engine.Serialization.Binary;

public ref struct SerializationReader
{
    private static readonly Encoding Utf8 =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    private readonly ReadOnlySpan<byte> _data;
    private readonly SerializationContext _context;

    private int _position;

    public SerializationReader(
        ReadOnlySpan<byte> data)
        : this(
            data,
            SerializationContext.Default)
    {
    }

    public SerializationReader(
        ReadOnlySpan<byte> data,
        SerializationContext context)
    {
        if (data.Length >
            context.MaxPayloadBytes)
        {
            throw new InvalidDataException(
                $"Serialized payload length '{data.Length}' exceeds the maximum allowed payload size '{context.MaxPayloadBytes}'.");
        }

        _data =
            data;

        _context =
            context;

        _position =
            0;
    }

    public SerializationContext Context =>
        _context;

    public int Position =>
        _position;

    public int Remaining =>
        _data.Length - _position;

    public bool IsAtEnd =>
        _position == _data.Length;

    public byte ReadByte()
    {
        EnsureAvailable(1);

        return _data[_position++];
    }

    public bool ReadBoolean()
    {
        return ReadByte() switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException(
                "Serialized boolean must be 0 or 1.")
        };
    }

    public int ReadInt32()
    {
        var span =
            ReadSpan(sizeof(int));

        return BinaryPrimitives.ReadInt32LittleEndian(
            span);
    }

    public uint ReadUInt32()
    {
        var span =
            ReadSpan(sizeof(uint));

        return BinaryPrimitives.ReadUInt32LittleEndian(
            span);
    }

    public long ReadInt64()
    {
        var span =
            ReadSpan(sizeof(long));

        return BinaryPrimitives.ReadInt64LittleEndian(
            span);
    }

    public ulong ReadUInt64()
    {
        var span =
            ReadSpan(sizeof(ulong));

        return BinaryPrimitives.ReadUInt64LittleEndian(
            span);
    }

    public float ReadSingle()
    {
        return BitConverter.Int32BitsToSingle(
            ReadInt32());
    }

    public double ReadDouble()
    {
        return BitConverter.Int64BitsToDouble(
            ReadInt64());
    }

    public byte[] ReadBytes()
    {
        var length =
            ReadLength(
                Context.MaxCollectionLength);

        if (length == 0)
        {
            return Array.Empty<byte>();
        }

        return ReadSpan(
                length)
            .ToArray();
    }

    public string? ReadString()
    {
        var length =
            ReadLength(
                Context.MaxStringBytes,
                allowNull: true);

        if (length == -1)
        {
            return null;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        return Utf8.GetString(
            ReadSpan(length));
    }

    private int ReadLength(
        int maximum,
        bool allowNull = false)
    {
        var length =
            ReadInt32();

        if (allowNull &&
            length == -1)
        {
            return -1;
        }

        if (length < 0)
        {
            throw new InvalidDataException(
                $"Serialized length '{length}' is invalid.");
        }

        if (length >
            maximum)
        {
            throw new InvalidDataException(
                $"Serialized length '{length}' exceeds the maximum allowed length '{maximum}'.");
        }

        return length;
    }

    private ReadOnlySpan<byte> ReadSpan(
        int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length));
        }

        EnsureAvailable(
            length);

        var span =
            _data.Slice(
                _position,
                length);

        _position +=
            length;

        return span;
    }

    private void EnsureAvailable(
        int count)
    {
        if (count > Remaining)
        {
            throw new InvalidDataException(
                "Serialized data ended before the expected value was fully read.");
        }
    }
}