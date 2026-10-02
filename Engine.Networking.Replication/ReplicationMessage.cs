namespace Engine.Networking.Replication;

public readonly record struct ReplicationMessage
{
    private ReplicationMessage(
        ReplicationOperation operation,
        ReplicatedEntityState state)
    {
        if (!Enum.IsDefined(operation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(state);

        if (operation == ReplicationOperation.Despawn &&
            state.Components.Count != 0)
        {
            throw new ArgumentException(
                "Despawn replication message cannot contain components.",
                nameof(state));
        }

        Operation =
            operation;

        State =
            state;
    }

    public ReplicationOperation Operation { get; }

    public ReplicatedEntityState State { get; }

    public static ReplicationMessage Spawn(
        ReplicatedEntityState state)
    {
        return new ReplicationMessage(
            ReplicationOperation.Spawn,
            state);
    }

    public static ReplicationMessage Update(
        ReplicatedEntityState state)
    {
        return new ReplicationMessage(
            ReplicationOperation.Update,
            state);
    }

    public static ReplicationMessage Despawn(
        NetworkEntityId networkId)
    {
        return new ReplicationMessage(
            ReplicationOperation.Despawn,
            new ReplicatedEntityState(
                networkId,
                Array.Empty<ReplicatedComponentState>()));
    }
}