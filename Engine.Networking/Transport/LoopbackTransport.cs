using Engine.Networking.Connections;
using Engine.Networking.Packets;

namespace Engine.Networking.Transport;

public sealed class LoopbackTransport : INetworkTransport
{
    private static readonly object Sync =
        new();

    private static readonly Dictionary<
        NetworkEndpoint,
        LoopbackTransport> Transports =
        new();

    private readonly Dictionary<
        ConnectionId,
        Queue<NetworkPacket>> _incoming =
        new();

    private readonly Dictionary<
        ConnectionId,
        PeerConnection> _peers =
        new();

    private readonly Queue<PendingAccept> _accepts =
        new();

    private ulong _nextConnectionId = 1;

    private NetworkEndpoint? _endpoint;

    private bool _running;
    private bool _disposed;

    public bool IsRunning =>
        _running;

    public void Start(
        NetworkEndpoint endpoint)
    {
        EnsureNotDisposed();

        if (_running)
        {
            throw new InvalidOperationException(
                "Transport is already running.");
        }

        lock (Sync)
        {
            if (Transports.ContainsKey(endpoint))
            {
                throw new InvalidOperationException(
                    $"A loopback transport is already listening on '{endpoint.Host}:{endpoint.Port}'.");
            }

            Transports.Add(
                endpoint,
                this);

            _endpoint =
                endpoint;

            _running = true;
        }
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        lock (Sync)
        {
            if (!_running)
            {
                return;
            }

            foreach (var pair in _peers.ToArray())
            {
                var peer =
                    pair.Value;

                peer.Transport._incoming.Remove(
                    peer.Connection);

                peer.Transport._peers.Remove(
                    peer.Connection);

                peer.Transport.RemovePendingAccept(
                    peer.Connection);
            }

            _incoming.Clear();
            _peers.Clear();
            _accepts.Clear();

            if (_endpoint.HasValue &&
                Transports.TryGetValue(
                    _endpoint.Value,
                    out var registered) &&
                ReferenceEquals(
                    registered,
                    this))
            {
                Transports.Remove(
                    _endpoint.Value);
            }

            _endpoint = null;
            _running = false;
        }
    }

    public ConnectionId Connect(
        NetworkEndpoint endpoint)
    {
        EnsureRunning();

        lock (Sync)
        {
            if (!Transports.TryGetValue(
                    endpoint,
                    out var remoteTransport))
            {
                throw new InvalidOperationException(
                    $"No loopback transport is listening on '{endpoint.Host}:{endpoint.Port}'.");
            }

            if (ReferenceEquals(
                    remoteTransport,
                    this))
            {
                throw new InvalidOperationException(
                    "A loopback transport cannot connect to itself.");
            }

            if (!remoteTransport._running)
            {
                throw new InvalidOperationException(
                    "Remote loopback transport is not running.");
            }

            var localConnection =
                new ConnectionId(
                    _nextConnectionId++);

            var remoteConnection =
                new ConnectionId(
                    remoteTransport._nextConnectionId++);

            _incoming.Add(
                localConnection,
                new Queue<NetworkPacket>());

            remoteTransport._incoming.Add(
                remoteConnection,
                new Queue<NetworkPacket>());

            _peers.Add(
                localConnection,
                new PeerConnection(
                    remoteTransport,
                    remoteConnection));

            remoteTransport._peers.Add(
                remoteConnection,
                new PeerConnection(
                    this,
                    localConnection));

            remoteTransport._accepts.Enqueue(
                new PendingAccept(
                    remoteConnection,
                    _endpoint!.Value));

            return localConnection;
        }
    }

    public bool TryAccept(
        out ConnectionId connection,
        out NetworkEndpoint remoteEndpoint)
    {
        EnsureRunning();

        lock (Sync)
        {
            if (_accepts.TryDequeue(
                    out var pending))
            {
                connection =
                    pending.Connection;

                remoteEndpoint =
                    pending.RemoteEndpoint;

                return true;
            }
        }

        connection =
            ConnectionId.Invalid;

        remoteEndpoint =
            default;

        return false;
    }

    public void Disconnect(
        ConnectionId connection)
    {
        EnsureRunning();

        lock (Sync)
        {
            _incoming.Remove(
                connection);

            if (!_peers.Remove(
                    connection,
                    out var peer))
            {
                return;
            }

            peer.Transport._incoming.Remove(
                peer.Connection);

            peer.Transport._peers.Remove(
                peer.Connection);

            peer.Transport.RemovePendingAccept(
                peer.Connection);
        }
    }

    public bool Send(
        ConnectionId connection,
        NetworkPacket packet)
    {
        EnsureRunning();

        lock (Sync)
        {
            if (!_incoming.ContainsKey(
                    connection))
            {
                return false;
            }

            if (!_peers.TryGetValue(
                    connection,
                    out var peer))
            {
                return false;
            }

            if (!peer.Transport._incoming.TryGetValue(
                    peer.Connection,
                    out var queue))
            {
                return false;
            }

            queue.Enqueue(
                packet);

            return true;
        }
    }

    public bool TryReceive(
        out ConnectionId connection,
        out NetworkPacket packet)
    {
        EnsureRunning();

        lock (Sync)
        {
            foreach (var pair in _incoming)
            {
                if (pair.Value.TryDequeue(
                        out packet))
                {
                    connection =
                        pair.Key;

                    return true;
                }
            }
        }

        connection =
            ConnectionId.Invalid;

        packet =
            default;

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();

        _disposed = true;
    }

    private void RemovePendingAccept(
        ConnectionId connection)
    {
        if (_accepts.Count == 0)
        {
            return;
        }

        var remaining =
            new Queue<PendingAccept>(
                _accepts.Count);

        while (_accepts.TryDequeue(
                   out var pending))
        {
            if (pending.Connection != connection)
            {
                remaining.Enqueue(
                    pending);
            }
        }

        while (remaining.TryDequeue(
                   out var pending))
        {
            _accepts.Enqueue(
                pending);
        }
    }

    private void EnsureRunning()
    {
        EnsureNotDisposed();

        if (!_running)
        {
            throw new InvalidOperationException(
                "Transport is not running.");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private readonly record struct PeerConnection(
        LoopbackTransport Transport,
        ConnectionId Connection);

    private readonly record struct PendingAccept(
        ConnectionId Connection,
        NetworkEndpoint RemoteEndpoint);
}