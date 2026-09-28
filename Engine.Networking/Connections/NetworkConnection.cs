namespace Engine.Networking.Connections;

public sealed class NetworkConnection
{
    internal NetworkConnection(
        ConnectionId id,
        NetworkEndpoint endpoint,
        NetworkConnectionDirection direction)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Connection ID must be valid.",
                nameof(id));
        }

        Id =
            id;

        Endpoint =
            endpoint;

        Direction =
            direction;

        State =
            NetworkConnectionState.Connecting;
    }

    public ConnectionId Id { get; }

    public NetworkEndpoint Endpoint { get; }

    public NetworkConnectionDirection Direction { get; }

    public NetworkConnectionState State { get; internal set; }
}