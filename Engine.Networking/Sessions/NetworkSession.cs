using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Transport;

namespace Engine.Networking.Sessions;

public sealed class NetworkSession :
    IDisposable
{
    private readonly INetworkTransport _transport;
    private NetworkEndpoint? _localEndpoint;
    private readonly Dictionary<
        ConnectionId,
        NetworkConnection> _connections =
        new();

    private bool _started;
    private bool _disposed;

    public NetworkSession(
        INetworkTransport transport)
    {
        ArgumentNullException.ThrowIfNull(
            transport);

        _transport =
            transport;
    }

    public bool IsStarted =>
        _started;

    public NetworkEndpoint LocalEndpoint
    {
        get
        {
            EnsureStarted();

            return _localEndpoint!.Value;
        }
    }

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

        _localEndpoint =
            endpoint;

        _started =
            true;


    }

    public NetworkConnection Connect(
        NetworkEndpoint endpoint)
    {
        EnsureStarted();

        if (TryGetConnection(
                endpoint,
                out _))
        {
            throw new InvalidOperationException(
                $"A connection to '{endpoint.Host}:{endpoint.Port}' already exists.");
        }

        var connectionId =
     _transport.Connect(
         endpoint);

        if (!connectionId.IsValid)
        {
            throw new InvalidOperationException(
                "Network transport returned an invalid connection ID.");
        }

        if (_connections.ContainsKey(
                connectionId))
        {
            throw new InvalidOperationException(
                $"Network transport returned an already registered connection ID '{connectionId.Value}'.");
        }

        var connection =
            new NetworkConnection(
                connectionId,
                endpoint,
                NetworkConnectionDirection.Outbound);

        connection.State =
            NetworkConnectionState.Connected;

        _connections.Add(
            connectionId,
            connection);

        ConnectionConnected?.Invoke(
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

        ConnectionDisconnected?.Invoke(
            networkConnection);

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

        var capability =
    packet.Channel switch
    {
        NetworkChannel.Reliable =>
            NetworkTransportCapabilities.Reliable,

        NetworkChannel.Unreliable =>
            NetworkTransportCapabilities.Unreliable,

        _ =>
            NetworkTransportCapabilities.None
    };

        if ((_transport.Capabilities & capability) == 0)
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
            _transport.TryReceiveDisconnect(
                out var disconnected))
        {
            if (!_connections.Remove(
                    disconnected,
                    out var connection))
            {
                continue;
            }

            connection.State =
                NetworkConnectionState.Disconnected;

            ConnectionDisconnected?.Invoke(
                connection);
        }

        while (
    _transport.TryAccept(
        out var connectionId,
        out var remoteEndpoint))
        {
            if (!connectionId.IsValid)
            {
                continue;
            }

            if (_connections.ContainsKey(
                    connectionId))
            {
                continue;
            }

            var connection =
                new NetworkConnection(
                    connectionId,
                    remoteEndpoint,
                    NetworkConnectionDirection.Inbound);

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
            if (!_connections.ContainsKey(
                    connection))
            {
                continue;
            }

            PacketReceived?.Invoke(
                connection,
                packet);
        }
    }

    public event Action<
        NetworkConnection>? ConnectionConnected;

    public event Action<
        NetworkConnection>? ConnectionAccepted;

    public event Action<
        NetworkConnection>? ConnectionDisconnected;

    public event Action<
        ConnectionId,
        NetworkPacket>? PacketReceived;

    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        var connections =
            _connections.Values.ToArray();

        foreach (var connection in
                 connections)
        {
            connection.State =
                NetworkConnectionState.Disconnecting;

            _transport.Disconnect(
                connection.Id);

            connection.State =
                NetworkConnectionState.Disconnected;

            ConnectionDisconnected?.Invoke(
                connection);
        }

        _connections.Clear();

        _transport.Stop();

        _localEndpoint =
            null;

        _started =
            false;
    }

    public bool TryGetConnection(
    ConnectionId id,
    out NetworkConnection? connection)
    {
        EnsureStarted();

        return _connections.TryGetValue(
            id,
            out connection);
    }

    public bool TryGetConnection(
        NetworkEndpoint endpoint,
        out NetworkConnection? connection)
    {
        EnsureStarted();

        foreach (var candidate in
                 _connections.Values)
        {
            if (candidate.Endpoint ==
                endpoint)
            {
                connection =
                    candidate;

                return true;
            }
        }

        connection =
            null;

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();

        _transport.Dispose();

        _disposed =
            true;
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