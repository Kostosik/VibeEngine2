using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Replication;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;
using Engine.Serialization.Binary;
using Engine.Serialization.Types;
using System.Diagnostics;

namespace Game.Sandbox;

public sealed class SandboxPlayerReplicationProbe : IDisposable
{
    private static readonly int BasePort =
        51000 + (Environment.ProcessId % 7000) * 2;

    private static readonly NetworkEndpoint ServerEndpoint =
        new("127.0.0.1", BasePort);

    private static readonly NetworkEndpoint ClientEndpoint =
        new("127.0.0.1", BasePort + 1);

    private static readonly NetworkEntityId PlayerNetworkId =
        new(9101);

    private NetworkSession? _serverSession;
    private NetworkSession? _clientSession;

    // Этот мир принадлежит EngineRuntime.
    // Тест не должен его освобождать.
    private Engine.ECS.World? _serverWorld;

    private bool _liveUpdateReceived;
    private long _liveUpdateStartedAt;

    // Клиентский мир принадлежит только тесту.
    private Engine.ECS.World? _clientWorld;

    private NetworkEntityMap? _serverMap;
    private NetworkEntityMap? _clientMap;

    private ReplicationStateService? _serverStateService;
    private ReplicationChannel? _serverChannel;
    private ReplicationChannel? _clientChannel;

    private EntityId _serverPlayer;
    private EntityId _clientPlayer;

    private ConnectionId _serverConnection;

    private FixedVector2 _initialPosition;
    private FixedVector2 _lastSentPosition;

    private long _startedAt;

    private bool _spawnReceived;
    private bool _awaitingUpdate;
    private bool _disposed;

    public bool IsRunning =>
        _serverSession is not null;

    public string Status { get; private set; } =
        "PLAYER REPLICATION: IDLE";

    public event Action<string>? StatusChanged;

    public void RunTest(
        Engine.ECS.World serverWorld,
        EntityId player)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        StopSessions();

        ArgumentNullException.ThrowIfNull(serverWorld);

        if (!serverWorld.Exists(player) ||
            !serverWorld.Has<WorldTransform2D>(player))
        {
            SetStatus(
                "PLAYER REPLICATION: PLAYER NOT FOUND");

            return;
        }

        try
        {
            _serverWorld = serverWorld;
            _serverPlayer = player;

            _initialPosition =
                serverWorld.Get<WorldTransform2D>(
                    player).Position;

            _lastSentPosition = _initialPosition;

            _serverSession =
                new NetworkSession(
                    new ReliableNetworkTransport(
                        new UdpTransport()));

            _clientSession =
                new NetworkSession(
                    new ReliableNetworkTransport(
                        new UdpTransport()));

            _clientWorld =
                new Engine.ECS.World();

            _serverMap =
                new NetworkEntityMap(
                    serverWorld);

            _clientMap =
                new NetworkEntityMap(
                    _clientWorld);

            _serverMap.Register(
                player,
                PlayerNetworkId);

            var serverRegistry =
                CreateRegistry();

            var clientRegistry =
                CreateRegistry();

            _serverStateService =
                new ReplicationStateService(
                    serverRegistry);

            var clientStateService =
                new ReplicationStateService(
                    clientRegistry);

            var serverEntityService =
                new ReplicationEntityService(
                    _serverStateService);

            var clientEntityService =
                new ReplicationEntityService(
                    clientStateService);

            var serverNode =
                new NetworkNode(
                    new NetworkNodeId(1),
                    ServerEndpoint);

            var clientNode =
                new NetworkNode(
                    new NetworkNodeId(2),
                    ClientEndpoint);

            var serverAuthority =
                new FixedNetworkAuthority(
                    serverNode.Id,
                    serverNode.Id);

            var clientAuthority =
                new FixedNetworkAuthority(
                    clientNode.Id,
                    serverNode.Id);

            _serverChannel =
                new ReplicationChannel(
                    _serverSession,
                    serverWorld,
                    _serverMap,
                    _serverStateService,
                    serverEntityService,
                    serverAuthority,
                    serverNode,
                    SerializationContext.Default);

            _clientChannel =
                new ReplicationChannel(
                    _clientSession,
                    _clientWorld,
                    _clientMap,
                    clientStateService,
                    clientEntityService,
                    clientAuthority,
                    serverNode,
                    SerializationContext.Default);

            _serverSession.ConnectionAccepted +=
                OnServerConnectionAccepted;

            _serverSession.Start(
                ServerEndpoint);

            _clientSession.Start(
                ClientEndpoint);

            _clientSession.Connect(
                ServerEndpoint);

            _startedAt =
                Stopwatch.GetTimestamp();

            SetStatus(
                "PLAYER REPLICATION: CONNECTING");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            Cancel(
                $"PLAYER REPLICATION: START FAILED ({exception.GetType().Name})");
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
            _serverSession.Update();
            _clientSession.Update();

            if (_serverWorld is null ||
                !_serverWorld.Exists(_serverPlayer) ||
                !_serverWorld.Has<WorldTransform2D>(
                    _serverPlayer))
            {
                Cancel(
                    "PLAYER REPLICATION: SOURCE PLAYER REMOVED");

                return;
            }

            var serverPosition =
                _serverWorld.Get<WorldTransform2D>(
                    _serverPlayer).Position;

            // The server's ReplicationChannel sends Spawn
            // when it accepts the client connection.
            if (!_spawnReceived &&
                _clientMap!.TryGetEntity(
                    PlayerNetworkId,
                    out var clientEntity))
            {
                if (!_clientWorld!.Exists(clientEntity) ||
                    !_clientWorld.Has<WorldTransform2D>(
                        clientEntity))
                {
                    Cancel(
                        "PLAYER REPLICATION: INVALID SPAWN");

                    return;
                }

                _clientPlayer = clientEntity;
                _spawnReceived = true;

                SetStatus(
                    "PLAYER REPLICATION: SPAWN OK - MOVE WITH WASD");
            }

            if (!_spawnReceived)
            {
                if (Stopwatch.GetElapsedTime(
                        _startedAt) >=
                    TimeSpan.FromSeconds(5))
                {
                    Cancel(
                        "PLAYER REPLICATION: CONNECTION TIMEOUT");
                }

                return;
            }

            // Replicate the actual player's transform only
            // when its authoritative position changes.
            if (serverPosition != _lastSentPosition)
            {
                var state =
                    _serverStateService!.Capture(
                        _serverWorld,
                        _serverMap!,
                        _serverPlayer,
                        SerializationContext.Default);

                if (!_serverChannel!.SendUpdate(
                        _serverConnection,
                        state))
                {
                    Cancel(
                        "PLAYER REPLICATION: UPDATE SEND FAILED");

                    return;
                }

                _lastSentPosition = serverPosition;
                _awaitingUpdate = true;

                if (!_liveUpdateReceived)
                {
                    SetStatus(
                        "PLAYER REPLICATION: UPDATE SENT");
                }
            }

            if (!_awaitingUpdate ||
    !_clientWorld!.Exists(_clientPlayer) ||
    !_clientWorld.Has<WorldTransform2D>(
        _clientPlayer))
            {
                return;
            }

            var clientPosition =
                _clientWorld.Get<WorldTransform2D>(
                    _clientPlayer).Position;

            // Подтверждаем, что клиент получил
            // изменённую позицию игрока.
            if (!_liveUpdateReceived &&
                clientPosition != _initialPosition)
            {
                _liveUpdateReceived = true;
                _liveUpdateStartedAt =
                    Stopwatch.GetTimestamp();

                SetStatus(
                    "PLAYER REPLICATION: PASS - STREAMING");
            }

            // Продолжаем полноценный обмен данными
            // ещё 5 секунд после первой успешной репликации.
            if (_liveUpdateReceived &&
                Stopwatch.GetElapsedTime(
                    _liveUpdateStartedAt) >=
                TimeSpan.FromSeconds(5))
            {
                StopSessions();

                SetStatus(
                    "PLAYER REPLICATION: PASS - STREAMING COMPLETE");

                return;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            Cancel(
                $"PLAYER REPLICATION: ERROR ({exception.GetType().Name})");
        }
    }

    private void OnServerConnectionAccepted(
        NetworkConnection connection)
    {
        _serverConnection = connection.Id;
    }

    private static ReplicatedComponentRegistry CreateRegistry()
    {
        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "engine.transform2d",
            new WorldTransform2DSerializer());

        return registry;
    }

    private void SetStatus(string status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }

    public void ShowUnavailable()
    {
        Cancel(
            "PLAYER REPLICATION: TOP-DOWN ONLY");
    }

    public void Cancel(
        string status = "PLAYER REPLICATION: STOPPED")
    {
        StopSessions();
        SetStatus(status);
    }

    private void StopSessions()
    {
        var serverSession = _serverSession;
        var clientSession = _clientSession;

        _serverSession = null;
        _clientSession = null;

        if (serverSession is not null)
        {
            serverSession.ConnectionAccepted -=
                OnServerConnectionAccepted;
        }

        _clientChannel?.Dispose();
        _clientChannel = null;

        _serverChannel?.Dispose();
        _serverChannel = null;

        DisposeSafely(clientSession);
        DisposeSafely(serverSession);

        _clientWorld?.Dispose();
        _clientWorld = null;

        _serverWorld = null;
        _serverMap = null;
        _clientMap = null;
        _serverStateService = null;

        _serverPlayer = default;
        _clientPlayer = default;
        _serverConnection = ConnectionId.Invalid;

        _spawnReceived = false;
        _awaitingUpdate = false;

        _liveUpdateReceived = false;
        _liveUpdateStartedAt = 0;
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