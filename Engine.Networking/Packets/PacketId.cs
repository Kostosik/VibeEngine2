namespace Engine.Networking.Packets;

public readonly record struct PacketId(
    ushort Value)
{
    public bool IsValid =>
        Value != 0;

    public static PacketId Invalid =>
        default;
}