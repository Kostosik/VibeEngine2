using System.Buffers.Binary;

namespace Engine.Serialization.Binary;

public static class BinaryContainer
{
    private const uint Magic = 0x31534256; // "VBS1"

    private const int HeaderSize =
        sizeof(uint) +
        sizeof(int) +
        sizeof(int);

    public static byte[] Pack(
        ReadOnlySpan<byte> payload,
        SerializationContext context)
    {
        if (payload.Length >
            context.MaxPayloadBytes)
        {
            throw new InvalidDataException(
                $"Payload length '{payload.Length}' exceeds the maximum allowed payload size '{context.MaxPayloadBytes}'.");
        }

        var data =
            new byte[
                checked(HeaderSize + payload.Length)];

        var span =
            data.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(
            span,
            Magic);

        BinaryPrimitives.WriteInt32LittleEndian(
            span.Slice(4),
            context.FormatVersion);

        BinaryPrimitives.WriteInt32LittleEndian(
            span.Slice(8),
            payload.Length);

        payload.CopyTo(
            span.Slice(HeaderSize));

        return data;
    }

    public static ReadOnlySpan<byte> Unpack(
        ReadOnlySpan<byte> data,
        SerializationContext context)
    {
        if (data.Length <
            HeaderSize)
        {
            throw new InvalidDataException(
                "Serialized container is smaller than its header.");
        }

        var magic =
            BinaryPrimitives.ReadUInt32LittleEndian(
                data);

        if (magic != Magic)
        {
            throw new InvalidDataException(
                "Serialized container has an invalid magic value.");
        }

        var version =
            BinaryPrimitives.ReadInt32LittleEndian(
                data.Slice(4));

        if (version !=
            context.FormatVersion)
        {
            throw new InvalidDataException(
                $"Serialized format version '{version}' is not supported. Expected '{context.FormatVersion}'.");
        }

        var payloadLength =
            BinaryPrimitives.ReadInt32LittleEndian(
                data.Slice(8));

        if (payloadLength < 0)
        {
            throw new InvalidDataException(
                $"Serialized payload length '{payloadLength}' is invalid.");
        }

        if (payloadLength >
            context.MaxPayloadBytes)
        {
            throw new InvalidDataException(
                $"Serialized payload length '{payloadLength}' exceeds the maximum allowed payload size '{context.MaxPayloadBytes}'.");
        }

        var expectedLength =
            checked(HeaderSize + payloadLength);

        if (data.Length !=
            expectedLength)
        {
            throw new InvalidDataException(
                $"Serialized container length '{data.Length}' does not match the declared payload length '{payloadLength}'.");
        }

        return data.Slice(
            HeaderSize,
            payloadLength);
    }
}