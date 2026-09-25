namespace Engine.Networking.Connections;

public sealed class NetworkConnection
{
    internal NetworkConnection(
        ConnectionId id,
        NetworkEndpoint endpoint)
    {
        Id = id;
        Endpoint = endpoint;
        State = NetworkConnectionState.Connecting;
    }

    public ConnectionId Id { get; }

    public NetworkEndpoint Endpoint { get; }

    public NetworkConnectionState State { get; internal set; }
}