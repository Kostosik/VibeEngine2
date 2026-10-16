using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Replication;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;
using Engine.Serialization.Binary;
using System.Diagnostics;

namespace Game.Sandbox;

public sealed class SandboxReplicationProbe : IDisposable
{
    private static readonly int BasePort =
        30000 + (Environment.ProcessId % 10000) * 2;

    private static readonly NetworkEndpoint ServerEndpoint =
        new("127.0.0.1", BasePort);

    private static readonly NetworkEndpoint ClientEndpoint =
        new("127.0.0.1", BasePort + 1);

    private static readonly NetworkEntityId ProbeNetworkId =
        new(9001);

    private const int InitialValue = 10;
    private const int UpdatedValue = 20;

    private NetworkSession? _serverSession;
    private NetworkSession? _clientSession;

    private World? _serverWorld;
    private World? _clientWorld;

    private NetworkEntityMap? _serverMap;
    private NetworkEntityMap? _clientMap;

    private ReplicationStateService? _serverStateService;
    private ReplicationChannel? _serverChannel;
    private ReplicationChannel? _clientChannel;

    private EntityId _serverEntity;
    private EntityId _replicaEntity;
    private ConnectionId _serverConnection;

    private long _startedAt;
    private bool _spawnVerified;
    private bool _updateSent;
    private bool _despawnSent;
    private bool _disposed;

    public string Status { get; private set; } =
        "NETWORK REPLICATION: IDLE";

    public event Action<string>? StatusChanged;

    public void RunTest()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        StopSessions();

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

            _serverWorld = new World();
            _clientWorld = new World();

            _serverMap =
                new NetworkEntityMap(
                    _serverWorld);

            _clientMap =
                new NetworkEntityMap(
                    _clientWorld);

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

            _serverEntity =
                _serverWorld.CreateEntity();

            _serverMap.Register(
                _serverEntity,
                ProbeNetworkId);

            _serverWorld.Add(
                _serverEntity,
                new ProbeComponent(
                    InitialValue));

            _serverChannel =
                new ReplicationChannel(
                    _serverSession,
                    _serverWorld,
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
                "NETWORK REPLICATION: CONNECTING");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            FailAndStop(
                $"NETWORK REPLICATION: START FAILED ({exception.GetType().Name})");
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

            // Stage 1: the client must receive the
            // replicated entity and its initial component.
            if (!_spawnVerified &&
                _clientMap!.TryGetEntity(
                    ProbeNetworkId,
                    out var clientEntity))
            {
                if (!_clientWorld!.Exists(clientEntity) ||
                    !_clientWorld.Has<ProbeComponent>(
                        clientEntity))
                {
                    FailAndStop(
                        "NETWORK REPLICATION: INVALID SPAWN");

                    return;
                }

                _replicaEntity = clientEntity;

                var value =
                    _clientWorld.Get<ProbeComponent>(
                        clientEntity).Value;

                if (value != InitialValue)
                {
                    FailAndStop(
                        "NETWORK REPLICATION: INITIAL VALUE MISMATCH");

                    return;
                }

                _spawnVerified = true;

                // Stage 2: change the authoritative component
                // and send its new state to the client.
                _serverWorld!.Remove<ProbeComponent>(
                    _serverEntity);

                _serverWorld.Add(
                    _serverEntity,
                    new ProbeComponent(
                        UpdatedValue));

                var state =
                    _serverStateService!.Capture(
                        _serverWorld,
                        _serverMap!,
                        _serverEntity,
                        SerializationContext.Default);

                if (!_serverChannel!.SendUpdate(
                        _serverConnection,
                        state))
                {
                    FailAndStop(
                        "NETWORK REPLICATION: UPDATE SEND FAILED");

                    return;
                }

                _updateSent = true;

                SetStatus(
                    "NETWORK REPLICATION: UPDATE SENT");
            }

            // Wait until the new component value has
            // actually arrived on the client.
            if (_updateSent &&
                !_despawnSent &&
                _clientMap!.TryGetEntity(
                    ProbeNetworkId,
                    out var replica))
            {
                if (!_clientWorld!.Has<ProbeComponent>(
                        replica))
                {
                    FailAndStop(
                        "NETWORK REPLICATION: COMPONENT MISSING");

                    return;
                }

                var value =
                    _clientWorld.Get<ProbeComponent>(
                        replica).Value;

                if (value == UpdatedValue)
                {
                    if (!_serverChannel!.SendDespawn(
                            _serverConnection,
                            ProbeNetworkId))
                    {
                        FailAndStop(
                            "NETWORK REPLICATION: DESPAWN SEND FAILED");

                        return;
                    }

                    _despawnSent = true;

                    SetStatus(
                        "NETWORK REPLICATION: DESPAWN SENT");
                }
                else if (value != InitialValue)
                {
                    FailAndStop(
                        "NETWORK REPLICATION: UPDATED VALUE MISMATCH");

                    return;
                }
            }

            // Stage 3: the client must remove the replicated
            // entity, not merely its network-ID mapping.
            if (_despawnSent &&
                !_clientMap!.TryGetEntity(
                    ProbeNetworkId,
                    out _))
            {
                if (_clientWorld!.Exists(
                        _replicaEntity))
                {
                    FailAndStop(
                        "NETWORK REPLICATION: ENTITY STILL EXISTS");

                    return;
                }

                SetStatus(
                    "NETWORK REPLICATION: PASS - SPAWN / UPDATE / DESPAWN");

                StopSessions();
                return;
            }

            if (Stopwatch.GetElapsedTime(
                    _startedAt) >=
                TimeSpan.FromSeconds(5))
            {
                FailAndStop(
                    "NETWORK REPLICATION: TIMEOUT");
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            FailAndStop(
                $"NETWORK REPLICATION: ERROR ({exception.GetType().Name})");
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
            "sandbox.network.probe",
            new ProbeComponentSerializer());

        return registry;
    }

    private void SetStatus(string status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }

    private void FailAndStop(string status)
    {
        SetStatus(status);
        StopSessions();
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

        _serverWorld?.Dispose();
        _serverWorld = null;

        _clientMap = null;
        _serverMap = null;
        _serverStateService = null;

        _serverConnection = ConnectionId.Invalid;

        _spawnVerified = false;
        _updateSent = false;
        _despawnSent = false;
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

    private readonly record struct ProbeComponent(
        int Value);

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
            return new ProbeComponent(
                reader.ReadInt32());
        }
    }
}