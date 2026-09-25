using Engine.Networking.Packets;
using Engine.Serialization.Binary;

namespace Engine.Networking.Messaging;

public interface INetworkMessageSerializer
{
    PacketId Id { get; }

    Type MessageType { get; }

    ReadOnlyMemory<byte> Serialize(
        object message,
        SerializationContext context);

    object Deserialize(
        ReadOnlySpan<byte> payload,
        SerializationContext context);
}