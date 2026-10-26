
using System.Diagnostics;
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

namespace Game.Sandbox;

public sealed class SandboxMultiplayerPlayerProbe : IDisposable
{
    private const int PortOffset = 20;

    private static readonly NetworkEntityId PlayerNetworkId = new(9301);
    private NetworkIdentityMapSynchronizer? _identitySynchronizer;
    private static readonly TimeSpan UpdateInterval =
        TimeSpan.FromMilliseconds(50);

    private readonly SandboxNetworkRoleOptions _options;

    private NetworkSession? _session;
    private Engine.ECS.World? _world;
    private NetworkEntityMap? _entityMap;
    private ReplicationStateService? _stateService;
    private ReplicationChannel? _channel;

    private EntityId _serverPlayer;
    private Func<EntityId>? _playerProvider;
    private ConnectionId _connection;

    private FixedVector2 _lastSentPosition;
    private FixedVector2 _initialRemotePosition;
    private FixedVector2 _lastRemotePosition;
    private FixedVector2? _targetRemotePosition;
    private long _lastClientSmoothingAt;
    private long _startedAt;
    private long _lastSnapshotAt;

    private bool _spawnObserved;
    private bool _liveUpdateObserved;
    private bool _disposed;

    public SandboxMultiplayerPlayerProbe(
        SandboxNetworkRoleOptions options)
    {
        _options = options ??
            throw new ArgumentNullException(nameof(options));

        Status = "PLAYER STREAM: IDLE";
    }

    public string Status { get; private set; }

    public FixedVector2? RemotePosition { get; private set; }

    public event Action<string>? StatusChanged;
public void Start(
    Engine.ECS.World world,
    Func<EntityId> playerProvider)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(playerProvider);

        if (_session is not null)
        {
            throw new InvalidOperationException(
                "Player replication is already running.");
        }

        if (_options.Port > 65515 ||
            _options.LocalPort > 65515)
        {
            throw new ArgumentOutOfRangeException(
                nameof(_options),
                "Player replication port is out of range.");
        }

        _playerProvider = playerProvider;

        var localPlayer = playerProvider();

        var isServer =
            _options.Role == SandboxNetworkRole.Server;

        if (isServer &&
            (!world.Exists(localPlayer) ||
             !world.Has<WorldTransform2D>(localPlayer)))
        {
            SetStatus("SERVER PLAYER: PLAYER NOT FOUND");
            return;
        }

        _world = world;

        try
        {
            _serverPlayer = localPlayer;

            var serverEndpoint = new NetworkEndpoint(
                _options.Host,
                _options.Port + PortOffset);

            var clientEndpoint = new NetworkEndpoint(
                "127.0.0.1",
                _options.LocalPort + PortOffset);

            var serverNode = new NetworkNode(
                new NetworkNodeId(1),
                serverEndpoint);

            var clientNode = new NetworkNode(
                new NetworkNodeId(2),
                clientEndpoint);

            _session = new NetworkSession(
                new ReliableNetworkTransport(
                    new UdpTransport()));

            _entityMap = new NetworkEntityMap(world);

            var registry = new ReplicatedComponentRegistry();

            registry.Register(
                "engine.transform2d",
                new WorldTransform2DSerializer());

            _stateService = new ReplicationStateService(registry);

            var entityService =
                new ReplicationEntityService(_stateService);

            var authority = isServer
                ? new FixedNetworkAuthority(
                    serverNode.Id,
                    serverNode.Id)
                : new FixedNetworkAuthority(
                    clientNode.Id,
                    serverNode.Id);

            // Sandbox-пробник сохраняет фиксированный ID игрока.
            if (isServer)
            {
                if (world.Has<NetworkIdentity>(localPlayer))
                {
                    var identity =
                        world.Get<NetworkIdentity>(localPlayer);

                    if (identity.HasAssignedId &&
                        identity.Id != PlayerNetworkId)
                    {
                        throw new InvalidOperationException(
                            $"Player already has network ID " +
                            $"'{identity.Id.Value}', expected " +
                            $"'{PlayerNetworkId.Value}'.");
                    }

                    if (!identity.HasAssignedId)
                    {
                        world.Remove<NetworkIdentity>(localPlayer);
                        world.Add(
                            localPlayer,
                            new NetworkIdentity(PlayerNetworkId));
                    }
                }
                else
                {
                    world.Add(
                        localPlayer,
                        new NetworkIdentity(PlayerNetworkId));
                }

                _lastSentPosition =
                    world.Get<WorldTransform2D>(
                        localPlayer).Position;
            }

            _identitySynchronizer =
                new NetworkIdentityMapSynchronizer(
                    world,
                    _entityMap,
                    authority);

            // Синхронизируем NetworkIdentity с картой до подключения.
            _identitySynchronizer.Reconcile();

            _channel = new ReplicationChannel(
                _session,
                world,
                _entityMap,
                _stateService,
                entityService,
                authority,
                serverNode,
                SerializationContext.Default);

            _session.ConnectionAccepted += OnConnectionAccepted;
            _session.ConnectionDisconnected += OnConnectionDisconnected;

            _session.Start(
                isServer
                    ? new NetworkEndpoint(
                        _options.BindHost,
                        _options.Port + PortOffset)
                    : clientEndpoint);

            if (isServer)
            {
                SetStatus(
                    $"SERVER PLAYER: LISTENING " +
                    $"{_options.BindHost}:{_options.Port + PortOffset}");
            }
            else
            {
                var connection = _session.Connect(serverEndpoint);
                _connection = connection.Id;

                SetStatus(
                    $"CLIENT PLAYER: CONNECTING TO " +
                    $"{serverEndpoint.Host}:{serverEndpoint.Port}");
            }

            _startedAt = Stopwatch.GetTimestamp();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);

            StopResources();

            SetStatus(
                $"PLAYER STREAM: START FAILED " +
                $"({exception.GetType().Name})");
        }
    }

    public void Update()
    {
        if (_session is null || _world is null)
        {
            return;
        }

        try
        {
            _identitySynchronizer?.Reconcile();

            _session.Update();

            if (_options.Role == SandboxNetworkRole.Server)
            {
                UpdateServer();
            }
            else
            {
                UpdateClient();
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);

            FailAndStop(
                $"PLAYER STREAM: ERROR ({exception.GetType().Name})");
        }
    }

    private void OnConnectionAccepted(NetworkConnection connection)
    {
        if (_options.Role != SandboxNetworkRole.Server)
        {
            return;
        }

        _connection = connection.Id;

        if (_world is not null &&
            _world.Exists(_serverPlayer) &&
            _world.Has<WorldTransform2D>(_serverPlayer))
        {
            _lastSentPosition =
                _world.Get<WorldTransform2D>(
                    _serverPlayer).Position;
        }

        _lastSnapshotAt = Stopwatch.GetTimestamp();

        SetStatus(
            "SERVER PLAYER: CLIENT CONNECTED; SPAWN SENT");
    }

    private void OnConnectionDisconnected(NetworkConnection connection)
    {
        if (connection.Id != _connection)
        {
            return;
        }

        _connection = ConnectionId.Invalid;
        RemotePosition = null;

        SetStatus(
            _options.Role == SandboxNetworkRole.Server
                ? "SERVER PLAYER: WAITING FOR CLIENT"
                : "CLIENT PLAYER: DISCONNECTED");
    }

    private void UpdateServer()
    {
        if (!_connection.IsValid ||
            _world is null ||
            _entityMap is null ||
            _stateService is null ||
            _channel is null)
        {
            return;
        }

        if (!_world.Exists(_serverPlayer) ||
            !_world.Has<WorldTransform2D>(_serverPlayer))
        {
            var currentPlayer = _playerProvider?.Invoke() ?? default;

            if (!_world.Exists(currentPlayer) ||
                !_world.Has<WorldTransform2D>(currentPlayer))
            {
                SetStatus("SERVER PLAYER: WAITING FOR PLAYER");
                return;
            }

            if (_serverPlayer != currentPlayer ||
                !_world.Exists(_serverPlayer) ||
                !_world.Has<WorldTransform2D>(_serverPlayer))
            {
                if (!RebindServerPlayer(currentPlayer))
                {
                    return;
                }
            }
        }

        var now = Stopwatch.GetTimestamp();

        if (Stopwatch.GetElapsedTime(_lastSnapshotAt, now) <
            UpdateInterval)
        {
            return;
        }

        _lastSnapshotAt = now;

        var position =
            _world.Get<WorldTransform2D>(
                _serverPlayer).Position;

        if (position == _lastSentPosition)
        {
            return;
        }

        var state = _stateService.Capture(
            _world,
            _entityMap,
            _serverPlayer,
            SerializationContext.Default);

        if (!_channel.SendUpdate(_connection, state))
        {
            FailAndStop("SERVER PLAYER: UPDATE SEND FAILED");
            return;
        }

        _lastSentPosition = position;

        if (!_liveUpdateObserved)
        {
            _liveUpdateObserved = true;
            SetStatus("SERVER PLAYER: LIVE TRANSFORM UPDATES SENT");
        }
    }


    private void UpdateClient()
    {
        if (_world is null || _entityMap is null)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();

        if (!_entityMap.TryGetEntity(
                PlayerNetworkId,
                out var remotePlayer))
        {
            SmoothRemotePosition(now);

            if (!_spawnObserved &&
                Stopwatch.GetElapsedTime(_startedAt) >=
                TimeSpan.FromSeconds(30))
            {
                FailAndStop("CLIENT PLAYER: SPAWN TIMEOUT");
            }

            return;
        }

        if (!_world.Exists(remotePlayer) ||
            !_world.Has<WorldTransform2D>(remotePlayer))
        {
            FailAndStop("CLIENT PLAYER: INVALID REMOTE PLAYER");
            return;
        }

        var position =
            _world.Get<WorldTransform2D>(
                remotePlayer).Position;

        if (!_spawnObserved)
        {
            _spawnObserved = true;

            _initialRemotePosition = position;
            _lastRemotePosition = position;
            _targetRemotePosition = position;

            // При первом появлении не интерполируем
            // от нулевой координаты.
            RemotePosition = position;
            _lastClientSmoothingAt = now;

            SetStatus("CLIENT PLAYER: REMOTE PLAYER SPAWNED");
            return;
        }

        // Репликация обновляет целевую позицию,
        // а отображаемая позиция движется к ней плавно.
        if (position != _lastRemotePosition)
        {
            _lastRemotePosition = position;
            _targetRemotePosition = position;

            if (!_liveUpdateObserved &&
                position != _initialRemotePosition)
            {
                _liveUpdateObserved = true;

                SetStatus(
                    "CLIENT PLAYER: PASS - LIVE TRANSFORM STREAM");
            }
        }

        SmoothRemotePosition(now);
    }

    private void SmoothRemotePosition(long now)
    {
        if (_targetRemotePosition is not { } target)
        {
            return;
        }

        if (RemotePosition is not { } current)
        {
            RemotePosition = target;
            _lastClientSmoothingAt = now;
            return;
        }

        var elapsedSeconds =
            (now - _lastClientSmoothingAt) /
            (double)Stopwatch.Frequency;

        _lastClientSmoothingAt = now;

        if (elapsedSeconds <= 0.0)
        {
            return;
        }

        // Экспоненциальное сглаживание независимо от FPS.
        // Коэффициент определяет скорость приближения к цели.
        var alpha =
            1.0f - MathF.Exp(
                -18.0f *
                (float)Math.Min(elapsedSeconds, 0.1));

        RemotePosition =
            current +
            (target - current) * Fixed32.FromFloat(alpha);
    }


    private void SetStatus(string status)
    {
        Status = status;

        Console.WriteLine(
            $"[MULTIPLAYER][{_options.Role}] {status}");

        StatusChanged?.Invoke(status);
    }

    private void FailAndStop(string status)
    {
        StopResources();
        SetStatus(status);
    }

    private bool RebindServerPlayer(EntityId currentPlayer)
    {
        if (_world is null ||
            _entityMap is null ||
            _stateService is null ||
            _channel is null)
        {
            return false;
        }

        if (_entityMap.TryGetEntity(
                PlayerNetworkId,
                out var previousPlayer))
        {
            // Сначала отправляем удаление старой реплики.
            if (_connection.IsValid &&
                !_channel.SendDespawn(
                    _connection,
                    PlayerNetworkId))
            {
                FailAndStop(
                    "SERVER PLAYER: OLD REPLICA DESPAWN FAILED");

                return false;
            }

            _entityMap.Unregister(previousPlayer);
        }

        _serverPlayer = currentPlayer;

        if (_world.Has<NetworkIdentity>(_serverPlayer))
        {
            var identity = _world.Get<NetworkIdentity>(_serverPlayer);

            if (identity.HasAssignedId && identity.Id != PlayerNetworkId)
            {
                throw new InvalidOperationException(
                    $"Server player has unexpected network ID '{identity.Id.Value}'.");
            }

            if (!identity.HasAssignedId)
            {
                _world.Remove<NetworkIdentity>(_serverPlayer);
                _world.Add(
                    _serverPlayer,
                    new NetworkIdentity(PlayerNetworkId));
            }
        }
        else
        {
            _world.Add(
                _serverPlayer,
                new NetworkIdentity(PlayerNetworkId));
        }

        _identitySynchronizer?.Reconcile();

        if (!_entityMap.TryGetNetworkId(_serverPlayer, out var networkId) ||
            networkId != PlayerNetworkId)
        {
            throw new InvalidOperationException(
                "Server player was not registered by the identity synchronizer.");
        }

        _lastSentPosition =
            _world.Get<WorldTransform2D>(
                _serverPlayer).Position;

        // Если клиент уже подключён, сразу создаём новую реплику.
        if (_connection.IsValid)
        {
            var state = _stateService.Capture(
                _world,
                _entityMap,
                _serverPlayer,
                SerializationContext.Default);

            if (!_channel.SendSpawn(
                    _connection,
                    state))
            {
                FailAndStop(
                    "SERVER PLAYER: REPLACEMENT SPAWN FAILED");

                return false;
            }

            SetStatus(
                "SERVER PLAYER: REPLACEMENT SPAWN SENT");
        }
        else
        {
            SetStatus(
                "SERVER PLAYER: WAITING FOR CONNECTION");
        }

        return true;
    }

    private void StopResources()
    {
        var session = _session;
        _session = null;

        if (session is not null)
        {
            session.ConnectionAccepted -= OnConnectionAccepted;
            session.ConnectionDisconnected -= OnConnectionDisconnected;
        }
        _identitySynchronizer?.Dispose();
        _identitySynchronizer = null;
        _channel?.Dispose();
        _channel = null;

        if (session is not null)
        {
            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }
        }

        if (_world is not null && _entityMap is not null)
        {
            foreach (var mapping in _entityMap.GetMappings())
            {
                // The server player belongs to TopDownScenario.
                // Only the client's remote replica is ours to destroy.
                if (_options.Role == SandboxNetworkRole.Client &&
                    _world.Exists(mapping.Entity))
                {
                    _world.DestroyEntity(mapping.Entity);
                }

                _entityMap.Unregister(mapping.Entity);
            }
        }

        _entityMap = null;
        _stateService = null;
        _world = null;

        _connection = ConnectionId.Invalid;
        RemotePosition = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopResources();

        _disposed = true;
        StatusChanged = null;
    }
}
