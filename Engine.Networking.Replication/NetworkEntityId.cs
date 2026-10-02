namespace Engine.Networking.Replication;

public readonly record struct NetworkEntityId(
    ulong Value)
{
    public bool IsValid =>
        Value != 0;

    public static NetworkEntityId Invalid =>
        default;
}