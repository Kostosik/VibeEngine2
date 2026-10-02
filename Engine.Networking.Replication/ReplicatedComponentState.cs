namespace Engine.Networking.Replication;

public sealed class ReplicatedComponentState
{
    public ReplicatedComponentState(
        string id,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        Id =
            id;

        Payload =
            payload.ToArray();
    }

    public string Id { get; }

    public ReadOnlyMemory<byte> Payload { get; }
}