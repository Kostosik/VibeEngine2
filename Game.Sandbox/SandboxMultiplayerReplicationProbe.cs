
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Replication;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;
using Engine.Serialization.Binary;
using System.Diagnostics;

namespace Game.Sandbox;

public sealed class SandboxMultiplayerReplicationProbe : IDisposable
{
    private static readonly NetworkEntityId ProbeNetworkId = new(9201);

    private static readonly PacketId SpawnAckId = new(242);
    private static readonly PacketId UpdateAckId = new(243);
    private static readonly PacketId DespawnAckId = new(244);

    private static readonly byte[] AckPayload = [0x52, 0x50, 0x31];

    private const int InitialValue = 10;
    private const int UpdatedValue = 20;

    private readonly SandboxNetworkRoleOptions _options;

    private World? _world;
    private NetworkSession? _session;
    private NetworkEntityMap? _entityMap;
    private ReplicationStateService? _stateService;
    private ReplicationChannel? _channel;

    private EntityId _serverEntity;
    private EntityId _clientEntity;
    private ConnectionId _connection;

    private bool _serverEntityCreated;
    private bool _clientSpawnAckSent;
    private bool _clientUpdateAckSent;
    private bool _clientDespawnAckSent;
    private bool _updateSent;
    private bool _despawnSent;
    private bool _completed;
    private bool _disposed;

    private long _startedAt;

    public SandboxMultiplayerReplicationProbe(
        SandboxNetworkRoleOptions options)
    {
        _options = options ??
            throw new ArgumentNullException(nameof(options));

        Status = "MULTIPLAYER REPLICATION: IDLE";
    }

    public string Status { get; private set; }

    public event Action<string>? StatusChanged;

    public void Start(World world)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(world);

        if (_session is not null)
        {
            throw new InvalidOperationException(
                "Multiplayer replication probe is already running.");
        }

        _world = world;

        try
        {
            _session = new NetworkSession(
                new ReliableNetworkTransport(
                    new UdpTransport()));

            _entityMap = new NetworkEntityMap(world);

            var registry = new ReplicatedComponentRegistry();

            registry.Register(
                "sandbox.multiplayer.probe",
                new ProbeComponentSerializer());

            _stateService = new ReplicationStateService(registry);

            var entityService =
                new ReplicationEntityService(_stateService);

            var serverEndpoint = new NetworkEndpoint(
                _options.Host,
                _options.Port);

            var clientEndpoint = new NetworkEndpoint(
                "127.0.0.1",
                _options.LocalPort);

            var serverNode = new NetworkNode(
                new NetworkNodeId(1),
                serverEndpoint);

            var clientNode = new NetworkNode(
                new NetworkNodeId(2),
                clientEndpoint);

            if (_options.Role == SandboxNetworkRole.Server)
            {
                _serverEntity = world.CreateEntity();
                _serverEntityCreated = true;

                _entityMap.Register(
                    _serverEntity,
                    ProbeNetworkId);

                world.Add(
                    _serverEntity,
                    new ProbeComponent(InitialValue));
            }

            var authority = _options.Role == SandboxNetworkRole.Server
                ? new FixedNetworkAuthority(serverNode.Id, serverNode.Id)
                : new FixedNetworkAuthority(clientNode.Id, serverNode.Id);

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
            _session.PacketReceived += OnPacketReceived;

            var localEndpoint =
                _options.Role == SandboxNetworkRole.Server
                    ? new NetworkEndpoint(
                        _options.BindHost,
                        _options.Port)
                    : clientEndpoint;

            _session.Start(localEndpoint);

            if (_options.Role == SandboxNetworkRole.Client)
            {
                var connection = _session.Connect(serverEndpoint);
                _connection = connection.Id;

                SetStatus(
                    $"CLIENT REPLICATION: CONNECTING TO " +
                    $"{_options.Host}:{_options.Port}");
            }
            else
            {
                SetStatus(
                    $"SERVER REPLICATION: LISTENING " +
                    $"{_options.BindHost}:{_options.Port}");
            }

            _startedAt = Stopwatch.GetTimestamp();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);

            FailAndStop(
                $"MULTIPLAYER REPLICATION: START FAILED " +
                $"({exception.GetType().Name})");
        }
    }

    public void Update()
    {
        if (_session is null || _completed)
        {
            return;
        }

        try
        {
            _session.Update();

            if (_options.Role == SandboxNetworkRole.Client)
            {
                UpdateClient();
            }

            var shouldCheckTimeout =
                _options.Role == SandboxNetworkRole.Client ||
                _connection.IsValid;

            if (!_completed &&
                shouldCheckTimeout &&
                Stopwatch.GetElapsedTime(_startedAt) >=
                TimeSpan.FromSeconds(12))
            {
                FailAndStop(
                    "MULTIPLAYER REPLICATION: TIMEOUT");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);

            FailAndStop(
                $"MULTIPLAYER REPLICATION: ERROR " +
                $"({exception.GetType().Name})");
        }
    }

    private void OnConnectionAccepted(NetworkConnection connection)
    {
        if (_options.Role != SandboxNetworkRole.Server)
        {
            return;
        }

        _connection = connection.Id;
        _startedAt = Stopwatch.GetTimestamp();

        SetStatus(
            "SERVER REPLICATION: CLIENT CONNECTED; SPAWN SENT");
    }

    private void OnConnectionDisconnected(NetworkConnection connection)
    {
        if (connection.Id == _connection && !_completed)
        {
            FailAndStop(
                $"{RoleLabel}: CONNECTION DISCONNECTED");
        }
    }

    private void OnPacketReceived(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (_options.Role != SandboxNetworkRole.Server ||
            connection != _connection)
        {
            return;
        }

        if (packet.Channel != NetworkChannel.Reliable ||
            !packet.Payload.Span.SequenceEqual(AckPayload))
        {
            return;
        }

        if (packet.Id == SpawnAckId && !_updateSent)
        {
            SendUpdate();
        }
        else if (packet.Id == UpdateAckId && !_despawnSent)
        {
            SendDespawn();
        }
        else if (packet.Id == DespawnAckId && _despawnSent)
        {
            _completed = true;

            SetStatus(
                "SERVER REPLICATION: PASS - SPAWN / UPDATE / DESPAWN");
        }
    }

    private void SendUpdate()
    {
        if (_world is null ||
            _entityMap is null ||
            _stateService is null ||
            _channel is null ||
            !_world.Exists(_serverEntity))
        {
            FailAndStop(
                "SERVER REPLICATION: SOURCE ENTITY MISSING");
            return;
        }

        _world.Remove<ProbeComponent>(_serverEntity);
        _world.Add(_serverEntity, new ProbeComponent(UpdatedValue));

        var state = _stateService.Capture(
            _world,
            _entityMap,
            _serverEntity,
            SerializationContext.Default);

        if (!_channel.SendUpdate(_connection, state))
        {
            FailAndStop(
                "SERVER REPLICATION: UPDATE SEND FAILED");
            return;
        }

        _updateSent = true;

        SetStatus(
            "SERVER REPLICATION: UPDATE SENT (20)");
    }

    private void SendDespawn()
    {
        if (_world is null ||
            _entityMap is null ||
            _channel is null)
        {
            FailAndStop(
                "SERVER REPLICATION: STATE UNAVAILABLE");
            return;
        }

        if (!_channel.SendDespawn(_connection, ProbeNetworkId))
        {
            FailAndStop(
                "SERVER REPLICATION: DESPAWN SEND FAILED");
            return;
        }

        _despawnSent = true;

        _world.DestroyEntity(_serverEntity);
        _entityMap.Unregister(_serverEntity);
        _serverEntityCreated = false;

        SetStatus(
            "SERVER REPLICATION: DESPAWN SENT");
    }

    private void UpdateClient()
    {
        if (_world is null || _entityMap is null)
        {
            return;
        }

        if (_entityMap.TryGetEntity(
                ProbeNetworkId,
                out var entity))
        {
            _clientEntity = entity;

            if (!_world.Has<ProbeComponent>(entity))
            {
                FailAndStop(
                    "CLIENT REPLICATION: COMPONENT MISSING");
                return;
            }

            var value = _world.Get<ProbeComponent>(entity).Value;

            if (!_clientSpawnAckSent)
            {
                if (value != InitialValue)
                {
                    FailAndStop(
                        "CLIENT REPLICATION: INVALID INITIAL VALUE");
                    return;
                }

                if (SendAck(SpawnAckId))
                {
                    _clientSpawnAckSent = true;

                    SetStatus(
                        "CLIENT REPLICATION: SPAWN VERIFIED (10)");
                }

                return;
            }

            if (!_clientUpdateAckSent)
            {
                if (value == InitialValue)
                {
                    return;
                }

                if (value != UpdatedValue)
                {
                    FailAndStop(
                        "CLIENT REPLICATION: INVALID UPDATED VALUE");
                    return;
                }

                if (SendAck(UpdateAckId))
                {
                    _clientUpdateAckSent = true;

                    SetStatus(
                        "CLIENT REPLICATION: UPDATE VERIFIED (20)");
                }

                return;
            }
        }
        else if (_clientUpdateAckSent && !_clientDespawnAckSent)
        {
            if (_world.Exists(_clientEntity))
            {
                FailAndStop(
                    "CLIENT REPLICATION: DESPAWN DID NOT DESTROY ENTITY");
                return;
            }

            if (SendAck(DespawnAckId))
            {
                _clientDespawnAckSent = true;
                _completed = true;

                SetStatus(
                    "CLIENT REPLICATION: PASS - SPAWN / UPDATE / DESPAWN");
            }
        }
    }

    private bool SendAck(PacketId packetId)
    {
        if (_session is null || !_connection.IsValid)
        {
            FailAndStop(
                "CLIENT REPLICATION: NO CONNECTION");
            return false;
        }

        if (!_session.Send(
                _connection,
                new NetworkPacket(
                    packetId,
                    NetworkChannel.Reliable,
                    AckPayload)))
        {
            FailAndStop(
                "CLIENT REPLICATION: ACK SEND FAILED");
            return false;
        }

        return true;
    }

    private string RoleLabel =>
        _options.Role == SandboxNetworkRole.Server
            ? "SERVER REPLICATION"
            : "CLIENT REPLICATION";

    private void SetStatus(string status)
    {
        Status = status;

        Console.WriteLine(
            $"[MULTIPLAYER][{_options.Role}] {status}");

        StatusChanged?.Invoke(status);
    }

    private void FailAndStop(string status)
    {
        SetStatus(status);
        StopResources();
    }

    private void StopResources()
    {
        var session = _session;
        _session = null;

        if (session is not null)
        {
            session.ConnectionAccepted -= OnConnectionAccepted;
            session.ConnectionDisconnected -= OnConnectionDisconnected;
            session.PacketReceived -= OnPacketReceived;
        }

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
                if (_world.Exists(mapping.Entity))
                {
                    _world.DestroyEntity(mapping.Entity);
                }

                _entityMap.Unregister(mapping.Entity);
            }
        }

        _entityMap = null;
        _stateService = null;

        _serverEntityCreated = false;
        _connection = ConnectionId.Invalid;
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

    private readonly record struct ProbeComponent(int Value);

    private sealed class ProbeComponentSerializer :
        IBinarySerializer<ProbeComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            ProbeComponent value)
        {
            writer.WriteInt32(value.Value);
        }

        public ProbeComponent Deserialize(
            ref SerializationReader reader)
        {
            return new ProbeComponent(reader.ReadInt32());
        }
    }
}
