using System.Diagnostics;
using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;

namespace Game.Sandbox;

public sealed class SandboxNetworkProbe : IDisposable
{
    private static readonly int BasePort =
        20000 + (Environment.ProcessId % 4000) * 2;

    private static readonly NetworkEndpoint ServerEndpoint =
        new("127.0.0.1", BasePort);

    private static readonly NetworkEndpoint ClientEndpoint =
        new("127.0.0.1", BasePort + 1);

    private static readonly PacketId PingPacketId =
        new(240);

    private static readonly PacketId PongPacketId =
        new(241);

    private static readonly byte[] TestPayload =
    [
        0x56,
        0x45,
        0x32
    ];

    private NetworkSession? _serverSession;
    private NetworkSession? _clientSession;

    private ConnectionId _clientConnection;

    private bool _serverAcceptedClient;
    private bool _awaitingPong;
    private bool _disposed;

    private long _pingStartedAt;

    public string Status { get; private set; } =
        "NETWORK: IDLE";

    public event Action<string>? StatusChanged;

    public void RunTest()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        // Повторное нажатие отправляет новый ping
        // через уже установленное соединение.
        if (_serverSession is not null &&
            _clientSession is not null)
        {
            if (_serverAcceptedClient)
            {
                SendPing();
            }
            else
            {
                SetStatus(
                    "NETWORK: connecting...");
            }

            return;
        }

        try
        {
            _serverSession =
                new NetworkSession(
                    new ReliableNetworkTransport(
                        new UdpTransport()));

            _clientSession =
                new NetworkSession(
                    new ReliableNetworkTransport(
                        new UdpTransport()));

            _serverSession.ConnectionAccepted +=
                OnServerConnectionAccepted;

            _serverSession.PacketReceived +=
                OnServerPacketReceived;

            _clientSession.PacketReceived +=
                OnClientPacketReceived;

            _serverSession.Start(
                ServerEndpoint);

            _clientSession.Start(
                ClientEndpoint);

            var connection =
                _clientSession.Connect(
                    ServerEndpoint);

            _clientConnection =
                connection.Id;

            SetStatus(
                "NETWORK: waiting for connection...");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            SetStatus(
                $"NETWORK: START FAILED ({exception.GetType().Name})");

            StopSessions();
        }
    }

    public void Update()
    {
        if (_serverSession is null ||
            _clientSession is null)
        {
            return;
        }

        try
        {
            // Обе сессии должны обрабатывать входящие
            // пакеты и подтверждения доставки.
            _serverSession.Update();
            _clientSession.Update();

            if (_awaitingPong &&
                Stopwatch.GetElapsedTime(
                    _pingStartedAt) >=
                TimeSpan.FromSeconds(3))
            {
                _awaitingPong = false;

                SetStatus(
                    "NETWORK: TIMEOUT");

                StopSessions();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            SetStatus(
                $"NETWORK: ERROR ({exception.GetType().Name})");

            StopSessions();
        }
    }

    private void OnServerConnectionAccepted(
        NetworkConnection connection)
    {
        _serverAcceptedClient = true;

        SetStatus(
            "NETWORK: connected; sending ping...");

        SendPing();
    }

    private void OnServerPacketReceived(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (packet.Id != PingPacketId)
        {
            return;
        }

        if (packet.Channel != NetworkChannel.Reliable ||
            !packet.Payload.Span.SequenceEqual(
                TestPayload))
        {
            _awaitingPong = false;

            SetStatus(
                "NETWORK: INVALID PING");

            return;
        }

        var response =
            new NetworkPacket(
                PongPacketId,
                NetworkChannel.Reliable,
                packet.Payload.ToArray());

        if (_serverSession is null ||
            !_serverSession.Send(
                connection,
                response))
        {
            _awaitingPong = false;

            SetStatus(
                "NETWORK: FAILED TO SEND PONG");
        }
    }

    private void OnClientPacketReceived(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (packet.Id != PongPacketId)
        {
            return;
        }

        _awaitingPong = false;

        if (packet.Channel != NetworkChannel.Reliable ||
            !packet.Payload.Span.SequenceEqual(
                TestPayload))
        {
            SetStatus(
                "NETWORK: INVALID RESPONSE");

            return;
        }

        SetStatus(
            "NETWORK: PASS - reliable UDP round-trip");
    }

    private void SendPing()
    {
        if (_clientSession is null ||
            !_clientConnection.IsValid)
        {
            SetStatus(
                "NETWORK: NO CLIENT CONNECTION");

            return;
        }

        var packet =
            new NetworkPacket(
                PingPacketId,
                NetworkChannel.Reliable,
                TestPayload);

        if (!_clientSession.Send(
                _clientConnection,
                packet))
        {
            _awaitingPong = false;

            SetStatus(
                "NETWORK: FAILED TO SEND PING");

            return;
        }

        _pingStartedAt =
            Stopwatch.GetTimestamp();

        _awaitingPong = true;

        SetStatus(
            "NETWORK: ping sent; waiting for pong...");
    }

    private void SetStatus(string status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }

    private void StopSessions()
    {
        var client = _clientSession;
        var server = _serverSession;

        _clientSession = null;
        _serverSession = null;

        if (client is not null)
        {
            client.PacketReceived -=
                OnClientPacketReceived;
        }

        if (server is not null)
        {
            server.ConnectionAccepted -=
                OnServerConnectionAccepted;

            server.PacketReceived -=
                OnServerPacketReceived;
        }

        DisposeSafely(client);
        DisposeSafely(server);

        _clientConnection =
            ConnectionId.Invalid;

        _serverAcceptedClient = false;
        _awaitingPong = false;
    }

    private static void DisposeSafely(
        NetworkSession? session)
    {
        if (session is null)
        {
            return;
        }

        try
        {
            session.Dispose();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopSessions();

        _disposed = true;
        StatusChanged = null;
    }
}