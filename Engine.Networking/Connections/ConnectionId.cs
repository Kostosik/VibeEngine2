namespace Engine.Networking.Connections;

public readonly record struct ConnectionId(
    ulong Value)
{
    public bool IsValid =>
        Value != 0;

    public static ConnectionId Invalid =>
        default;
}