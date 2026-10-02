namespace Engine.Networking.Replication;

public struct NetworkIdentity
{
    public NetworkIdentity(
        NetworkEntityId id)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Network entity ID must be valid.",
                nameof(id));
        }

        Id =
            id;
    }

    public NetworkEntityId Id { get; }
}