
using System.Diagnostics;
using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;

namespace Game.Sandbox;

public sealed class SandboxMultiplayerProbe : IDisposable
{
    private static readonly PacketId PingPacketId = new(240);
    private static readonly PacketId PongPacketId = new(241);

    private static readonly byte[] TestPayload =
    [
        0x56,
        0x45,
        0x32
    ];

    private static readonly TimeSpan PingTimeout =
        TimeSpan.FromSeconds(3);

    private readonly SandboxNetworkRoleOptions _options;

    private NetworkSession? _session;
    private ConnectionId _connection;

    private bool _awaitingPong;
    private bool _disposed;

    private long _lastPingStartedAt;
    private long _nextPingAt;

    public SandboxMultiplayerProbe(
        SandboxNetworkRoleOptions options)
    {
        _options = options ??
            throw new ArgumentNullException(nameof(options));

        Status = options.Role == SandboxNetworkRole.Server
            ? "SERVER: NOT STARTED"
            : "CLIENT: NOT STARTED";
    }

    public string Status { get; private set; }

    public event Action<string>? StatusChanged;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_session is not null)
        {
            throw new InvalidOperationException(
                "Multiplayer probe has already started.");
        }

        try
        {
            _session = new NetworkSession(
                new ReliableNetworkTransport(
                    new UdpTransport()));

            _session.ConnectionAccepted +=
                OnConnectionAccepted;

            _session.ConnectionDisconnected +=
                OnConnectionDisconnected;

            _session.PacketReceived +=
                OnPacketReceived;

            var isServer =
                _options.Role == SandboxNetworkRole.Server;

            var localHost = isServer
                ? _options.BindHost
                : "127.0.0.1";

            var localPort = isServer
                ? _options.Port
                : _options.LocalPort;

            _session.Start(
                new NetworkEndpoint(localHost, localPort));

            if (isServer)
            {
                SetStatus(
                    $"SERVER: LISTENING {localHost}:{localPort}");

                return;
            }

            var connection = _session.Connect(
                new NetworkEndpoint(
                    _options.Host,
                    _options.Port));

            _connection = connection.Id;

            SetStatus(
                $"CLIENT: CONNECTING TO " +
                $"{_options.Host}:{_options.Port}");

            // Send the first ping from Update().
            // The connection control datagram is sent by Connect().
            _nextPingAt = Stopwatch.GetTimestamp();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);

            StopSession();

            SetStatus(
                $"{RoleLabel}: START FAILED " +
                $"({exception.GetType().Name})");
        }
    }

    public void Update()
    {
        if (_session is null)
        {
            return;
        }

        try
        {
            _session.Update();

            if (_options.Role != SandboxNetworkRole.Client)
            {
                return;
            }

            var now = Stopwatch.GetTimestamp();

            if (_awaitingPong &&
                Stopwatch.GetElapsedTime(_lastPingStartedAt) >=
                PingTimeout)
            {
                _awaitingPong = false;
                _nextPingAt = now + Stopwatch.Frequency;

                SetStatus("CLIENT: TIMEOUT - no PONG received");
                return;
            }

            if (!_awaitingPong && now >= _nextPingAt)
            {
                SendPing();
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);

            StopSession();

            SetStatus(
                $"{RoleLabel}: ERROR " +
                $"({exception.GetType().Name})");
        }
    }

    private void OnConnectionAccepted(
        NetworkConnection connection)
    {
        if (_options.Role != SandboxNetworkRole.Server)
        {
            return;
        }

        _connection = connection.Id;

        SetStatus(
            $"SERVER: CLIENT CONNECTED " +
            $"({connection.Endpoint.Host}:{connection.Endpoint.Port})");
    }

    private void OnConnectionDisconnected(
        NetworkConnection connection)
    {
        if (connection.Id == _connection)
        {
            _connection = ConnectionId.Invalid;
        }

        SetStatus(
            _options.Role == SandboxNetworkRole.Server
                ? "SERVER: CLIENT DISCONNECTED"
                : "CLIENT: DISCONNECTED");
    }

    private void OnPacketReceived(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (_options.Role == SandboxNetworkRole.Server)
        {
            HandleServerPacket(connection, packet);
            return;
        }

        HandleClientPacket(connection, packet);
    }

    private void HandleServerPacket(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (packet.Id != PingPacketId)
        {
            return;
        }

        if (packet.Channel != NetworkChannel.Reliable ||
            !packet.Payload.Span.SequenceEqual(TestPayload))
        {
            SetStatus("SERVER: INVALID PING");
            return;
        }

        var response = new NetworkPacket(
            PongPacketId,
            NetworkChannel.Reliable,
            packet.Payload.ToArray());

        if (_session is null ||
            !_session.Send(connection, response))
        {
            SetStatus("SERVER: FAILED TO SEND PONG");
            return;
        }

        _connection = connection;

        SetStatus("SERVER: PASS - PONG SENT");
    }

    private void HandleClientPacket(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (connection != _connection ||
            packet.Id != PongPacketId)
        {
            return;
        }

        _awaitingPong = false;

        if (packet.Channel != NetworkChannel.Reliable ||
            !packet.Payload.Span.SequenceEqual(TestPayload))
        {
            SetStatus("CLIENT: INVALID PONG");
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(
            _lastPingStartedAt);

        SetStatus(
            $"CLIENT: PASS - reliable UDP, " +
            $"RTT {elapsed.TotalMilliseconds:0.0} ms");
    }

    private void SendPing()
    {
        if (_session is null || !_connection.IsValid)
        {
            SetStatus("CLIENT: NO CONNECTION");
            _nextPingAt =
                Stopwatch.GetTimestamp() + Stopwatch.Frequency;
            return;
        }

        var packet = new NetworkPacket(
            PingPacketId,
            NetworkChannel.Reliable,
            TestPayload);

        if (!_session.Send(_connection, packet))
        {
            SetStatus("CLIENT: FAILED TO SEND PING");

            _nextPingAt =
                Stopwatch.GetTimestamp() + Stopwatch.Frequency;

            return;
        }

        _lastPingStartedAt = Stopwatch.GetTimestamp();
        _nextPingAt = _lastPingStartedAt + Stopwatch.Frequency;
        _awaitingPong = true;

        SetStatus("CLIENT: PING SENT - waiting for PONG");
    }

    private string RoleLabel =>
        _options.Role == SandboxNetworkRole.Server
            ? "SERVER"
            : "CLIENT";

    private void SetStatus(string status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }

    private void StopSession()
    {
        var session = _session;
        _session = null;

        if (session is null)
        {
            return;
        }

        session.ConnectionAccepted -= OnConnectionAccepted;
        session.ConnectionDisconnected -= OnConnectionDisconnected;
        session.PacketReceived -= OnPacketReceived;

        try
        {
            session.Dispose();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
        }

        _connection = ConnectionId.Invalid;
        _awaitingPong = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopSession();

        _disposed = true;
        StatusChanged = null;
    }
}
