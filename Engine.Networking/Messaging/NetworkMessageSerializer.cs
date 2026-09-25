using Engine.Networking.Packets;
using Engine.Serialization.Binary;

namespace Engine.Networking.Messaging;

public sealed class NetworkMessageSerializer<T> :
    INetworkMessageSerializer
{
    private readonly IBinarySerializer<T> _serializer;

    public NetworkMessageSerializer(
        PacketId id,
        IBinarySerializer<T> serializer)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Message ID must be valid.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(
            serializer);

        Id =
            id;

        _serializer =
            serializer;
    }

    public PacketId Id { get; }

    public Type MessageType =>
        typeof(T);

    public ReadOnlyMemory<byte> Serialize(
        object message,
        SerializationContext context)
    {
        if (message is not T value)
        {
            throw new ArgumentException(
                $"Message must be of type '{typeof(T).FullName}'.",
                nameof(message));
        }

        return BinarySerializer.Serialize(
            value,
            _serializer,
            context);
    }

    public object Deserialize(
        ReadOnlySpan<byte> payload,
        SerializationContext context)
    {
        return BinarySerializer.Deserialize(
            payload,
            _serializer,
            context);
    }
}