namespace Engine.Networking.Packets;

public readonly struct NetworkPacket
{
    public NetworkPacket(
        PacketId id,
        NetworkChannel channel,
        ReadOnlyMemory<byte> payload)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Packet ID must be valid.",
                nameof(id));
        }

        Payload = payload;
        Id = id;
        Channel = channel;
    }

    public PacketId Id { get; }

    public NetworkChannel Channel { get; }

    public ReadOnlyMemory<byte> Payload { get; }
}