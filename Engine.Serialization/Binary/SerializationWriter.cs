using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Engine.Serialization.Binary;

public struct SerializationWriter
{
    private static readonly Encoding Utf8 =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    private readonly ArrayBufferWriter<byte> _buffer;

    public SerializationWriter()
        : this(
            SerializationContext.Default,
            256)
    {
    }

    public SerializationWriter(
        SerializationContext context)
        : this(
            context,
            256)
    {
    }

    public SerializationWriter(
        SerializationContext context,
        int initialCapacity)
    {
        if (initialCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCapacity));
        }

        Context =
            context;

        _buffer =
            new ArrayBufferWriter<byte>(
                initialCapacity);
    }

    public SerializationContext Context { get; }

    public int Length =>
        _buffer.WrittenCount;

    public ReadOnlyMemory<byte> WrittenMemory =>
        _buffer.WrittenMemory;

    public ReadOnlySpan<byte> WrittenSpan =>
        _buffer.WrittenSpan;

    public void WriteByte(
        byte value)
    {
        EnsureCanWrite(1);

        var span =
            _buffer.GetSpan(1);

        span[0] =
            value;

        _buffer.Advance(1);
    }

    public void WriteBoolean(
        bool value)
    {
        WriteByte(
            value ? (byte)1 : (byte)0);
    }

    public void WriteInt32(
        int value)
    {
        EnsureCanWrite(
            sizeof(int));

        var span =
            _buffer.GetSpan(sizeof(int));

        BinaryPrimitives.WriteInt32LittleEndian(
            span,
            value);

        _buffer.Advance(
            sizeof(int));
    }

    public void WriteUInt32(
        uint value)
    {
        EnsureCanWrite(
            sizeof(uint));

        var span =
            _buffer.GetSpan(sizeof(uint));

        BinaryPrimitives.WriteUInt32LittleEndian(
            span,
            value);

        _buffer.Advance(
            sizeof(uint));
    }

    public void WriteInt64(
        long value)
    {
        EnsureCanWrite(
            sizeof(long));

        var span =
            _buffer.GetSpan(sizeof(long));

        BinaryPrimitives.WriteInt64LittleEndian(
            span,
            value);

        _buffer.Advance(
            sizeof(long));
    }

    public void WriteUInt64(
        ulong value)
    {
        EnsureCanWrite(
            sizeof(ulong));

        var span =
            _buffer.GetSpan(sizeof(ulong));

        BinaryPrimitives.WriteUInt64LittleEndian(
            span,
            value);

        _buffer.Advance(
            sizeof(ulong));
    }

    public void WriteSingle(
        float value)
    {
        EnsureCanWrite(
            sizeof(float));

        var span =
            _buffer.GetSpan(sizeof(float));

        BinaryPrimitives.WriteInt32LittleEndian(
            span,
            BitConverter.SingleToInt32Bits(value));

        _buffer.Advance(
            sizeof(float));
    }

    public void WriteDouble(
        double value)
    {
        EnsureCanWrite(
            sizeof(double));

        var span =
            _buffer.GetSpan(sizeof(double));

        BinaryPrimitives.WriteInt64LittleEndian(
            span,
            BitConverter.DoubleToInt64Bits(value));

        _buffer.Advance(
            sizeof(double));
    }

    public void WriteBytes(
        ReadOnlySpan<byte> value)
    {
        if (value.Length >
            Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Byte collection length '{value.Length}' exceeds the maximum allowed length '{Context.MaxCollectionLength}'.");
        }

        EnsureCanWrite(
            checked(sizeof(int) + value.Length));

        WriteInt32(
            value.Length);

        if (value.IsEmpty)
        {
            return;
        }

        var span =
            _buffer.GetSpan(
                value.Length);

        value.CopyTo(
            span);

        _buffer.Advance(
            value.Length);
    }

    public void WriteString(
        string? value)
    {
        if (value is null)
        {
            WriteInt32(-1);
            return;
        }

        var byteCount =
            Utf8.GetByteCount(
                value);

        if (byteCount >
            Context.MaxStringBytes)
        {
            throw new InvalidDataException(
                $"String length '{byteCount}' bytes exceeds the maximum allowed length '{Context.MaxStringBytes}'.");
        }

        EnsureCanWrite(
            checked(sizeof(int) + byteCount));

        WriteInt32(
            byteCount);

        if (byteCount == 0)
        {
            return;
        }

        var span =
            _buffer.GetSpan(
                byteCount);

        Utf8.GetBytes(
            value,
            span);

        _buffer.Advance(
            byteCount);
    }

    public byte[] ToArray()
    {
        return _buffer.WrittenSpan.ToArray();
    }

    private void EnsureCanWrite(
        int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count));
        }

        var newLength =
            checked(
                _buffer.WrittenCount +
                count);

        if (newLength >
            Context.MaxPayloadBytes)
        {
            throw new InvalidDataException(
                $"Serialized payload length '{newLength}' exceeds the maximum allowed payload size '{Context.MaxPayloadBytes}'.");
        }
    }
}