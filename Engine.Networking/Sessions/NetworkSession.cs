using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Transport;

namespace Engine.Networking.Sessions;

public sealed class NetworkSession : IDisposable
{
    private readonly INetworkTransport _transport;

    private readonly Dictionary<
        ConnectionId,
        NetworkConnection> _connections = new();

    private bool _started;
    private bool _disposed;

    public NetworkSession(
        INetworkTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        _transport = transport;
    }

    public bool IsStarted =>
        _started;

    public IReadOnlyCollection<NetworkConnection> Connections =>
        _connections.Values;

    public void Start(
        NetworkEndpoint endpoint)
    {
        EnsureNotDisposed();

        if (_started)
        {
            throw new InvalidOperationException(
                "Network session is already started.");
        }

        _transport.Start(
            endpoint);

        _started = true;
    }

    public NetworkConnection Connect(
        NetworkEndpoint endpoint)
    {
        EnsureStarted();

        var id =
            _transport.Connect(
                endpoint);

        var connection =
            new NetworkConnection(
                id,
                endpoint);

        connection.State =
            NetworkConnectionState.Connected;

        _connections.Add(
            id,
            connection);

        return connection;
    }

    public bool Disconnect(
        ConnectionId connection)
    {
        EnsureStarted();

        if (!_connections.Remove(
                connection,
                out var networkConnection))
        {
            return false;
        }

        networkConnection.State =
            NetworkConnectionState.Disconnecting;

        _transport.Disconnect(
            connection);

        networkConnection.State =
            NetworkConnectionState.Disconnected;

        return true;
    }

    public bool Send(
        ConnectionId connection,
        NetworkPacket packet)
    {
        EnsureStarted();

        if (!_connections.TryGetValue(
                connection,
                out var networkConnection))
        {
            return false;
        }

        if (networkConnection.State !=
            NetworkConnectionState.Connected)
        {
            return false;
        }

        return _transport.Send(
            connection,
            packet);
    }

    public bool TryReceive(
        out ConnectionId connection,
        out NetworkPacket packet)
    {
        EnsureStarted();

        return _transport.TryReceive(
            out connection,
            out packet);
    }

    public void Update()
    {
        EnsureStarted();

        while (
            _transport.TryAccept(
                out var connectionId,
                out var remoteEndpoint))
        {
            var connection =
                new NetworkConnection(
                    connectionId,
                    remoteEndpoint);

            connection.State =
                NetworkConnectionState.Connected;

            _connections.Add(
                connectionId,
                connection);

            ConnectionAccepted?.Invoke(
                connection);
        }

        while (
            _transport.TryReceive(
                out var connection,
                out var packet))
        {
            PacketReceived?.Invoke(
                connection,
                packet);
        }
    }

    public event Action<
        NetworkConnection>? ConnectionAccepted;

    public event Action<
        ConnectionId,
        NetworkPacket>? PacketReceived;

    public void Stop()
    {
        if (!_started)
            return;

        foreach (var connection in
                 _connections.Values)
        {
            connection.State =
                NetworkConnectionState.Disconnected;
        }

        _connections.Clear();

        _transport.Stop();

        _started = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();

        _transport.Dispose();

        _disposed = true;
    }

    private void EnsureStarted()
    {
        EnsureNotDisposed();

        if (!_started)
        {
            throw new InvalidOperationException(
                "Network session has not been started.");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}