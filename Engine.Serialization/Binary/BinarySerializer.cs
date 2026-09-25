namespace Engine.Serialization.Binary;

public static class BinarySerializer
{
    public static byte[] Serialize<T>(
        T value,
        IBinarySerializer<T> serializer)
    {
        return Serialize(
            value,
            serializer,
            SerializationContext.Default);
    }

    public static byte[] Serialize<T>(
        T value,
        IBinarySerializer<T> serializer,
        SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var writer =
            new SerializationWriter(
                context);

        serializer.Serialize(
            ref writer,
            value);

        return writer.ToArray();
    }

    public static byte[] SerializeContainer<T>(
        T value,
        IBinarySerializer<T> serializer,
        SerializationContext context)
    {
        var payload =
            Serialize(
                value,
                serializer,
                context);

        return BinaryContainer.Pack(
            payload,
            context);
    }

    public static T Deserialize<T>(
        ReadOnlySpan<byte> data,
        IBinarySerializer<T> serializer)
    {
        return Deserialize(
            data,
            serializer,
            SerializationContext.Default);
    }

    public static T Deserialize<T>(
        ReadOnlySpan<byte> data,
        IBinarySerializer<T> serializer,
        SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var reader =
            new SerializationReader(
                data,
                context);

        var value =
            serializer.Deserialize(
                ref reader);

        if (!reader.IsAtEnd)
        {
            throw new InvalidDataException(
                $"Serialized data contains {reader.Remaining} unread byte(s).");
        }

        return value;
    }

    public static T DeserializeContainer<T>(
        ReadOnlySpan<byte> data,
        IBinarySerializer<T> serializer,
        SerializationContext context)
    {
        var payload =
            BinaryContainer.Unpack(
                data,
                context);

        return Deserialize(
            payload,
            serializer,
            context);
    }
}