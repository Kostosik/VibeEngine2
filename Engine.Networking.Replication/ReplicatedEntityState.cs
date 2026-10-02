namespace Engine.Networking.Replication;

public sealed class ReplicatedEntityState
{
    public ReplicatedEntityState(
        NetworkEntityId id,
        IReadOnlyList<ReplicatedComponentState> components)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Network entity ID must be valid.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(
            components);

        Id =
            id;

        Components =
            components.ToArray();
    }

    public NetworkEntityId Id { get; }

    public IReadOnlyList<ReplicatedComponentState> Components { get; }
}