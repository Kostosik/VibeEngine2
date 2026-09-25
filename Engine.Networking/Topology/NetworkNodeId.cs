namespace Engine.Networking.Topology;

public readonly record struct NetworkNodeId
{
    public NetworkNodeId(
        ulong value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value));
        }

        Value =
            value;
    }

    public ulong Value { get; }

    public static NetworkNodeId Invalid =>
        default;

    public bool IsValid =>
        Value != 0;
}