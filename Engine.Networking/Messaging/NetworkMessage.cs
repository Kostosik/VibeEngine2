using Engine.Networking.Packets;

namespace Engine.Networking.Messaging;

public readonly record struct NetworkMessage<T>
{
    public NetworkMessage(
        PacketId id,
        T payload)
        : this(
            id,
            NetworkChannel.Reliable,
            payload)
    {
    }

    public NetworkMessage(
        PacketId id,
        NetworkChannel channel,
        T payload)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Message ID must be valid.",
                nameof(id));
        }

        Id =
            id;

        Channel =
            channel;

        Payload =
            payload;
    }

    public PacketId Id { get; }

    public NetworkChannel Channel { get; }

    public T Payload { get; }
}