using Engine.ECS;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Messaging;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Serialization.Binary;

namespace Engine.Networking.Replication;

public sealed class ReplicationChannel :
    IDisposable
{
    private static readonly PacketId ReplicationPacketId =
        new(3);
    private readonly SerializationContext _context;
    private readonly ReplicationEntityService _entityService;
    private readonly NetworkMessageChannel _messages;
    private readonly World _world;
    private readonly NetworkEntityMap _entityMap;
    private readonly NetworkNode _authorityNode;
    private readonly INetworkAuthority _authority;
    private bool _disposed;
    private readonly NetworkSession _session;
    private readonly ReplicationStateService _stateService;

    public ReplicationChannel(
        NetworkSession session,
        World world,
        NetworkEntityMap entityMap,
        ReplicationStateService stateService,
        ReplicationEntityService entityService,
        INetworkAuthority authority,
        NetworkNode authorityNode,
        SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityMap);
        ArgumentNullException.ThrowIfNull(entityService);
        ArgumentNullException.ThrowIfNull(authority);

        if (!authorityNode.Id.IsValid)
        {
            throw new ArgumentException(
                "Authority node ID must be valid.",
                nameof(authorityNode));
        }

        _stateService = stateService;

        _authority =
            authority;

        _authorityNode =
            authorityNode;
        _session =
    session;

        _world =
            world;

        _entityMap =
            entityMap;

        _context = context;

        _entityService =
            entityService;

        _messages =
            new NetworkMessageChannel(
                session,
                context);

        _messages.Register(
            ReplicationPacketId,
            new ReplicationMessageSerializer(),
            OnMessage);

        session.ConnectionConnected +=
    OnConnectionReady;

        session.ConnectionAccepted +=
            OnConnectionReady;
    }

    private void OnConnectionReady(
    NetworkConnection connection)
    {
        if (!_authority.HasAuthority)
        {
            return;
        }

        foreach (var mapping in
                 _entityMap.GetMappings())
        {
            if (!_world.Exists(
                    mapping.Entity))
            {
                continue;
            }

            var state =
                _stateService.Capture(
                    _world,
                    _entityMap,
                    mapping.Entity,
                    _context);

            SendSpawn(
                connection.Id,
                state);
        }
    }

    public bool SendSpawn(
        ConnectionId connection,
        ReplicatedEntityState state)
    {
        return Send(
            connection,
            ReplicationMessage.Spawn(
                state));
    }

    public bool SendUpdate(
        ConnectionId connection,
        ReplicatedEntityState state)
    {
        return Send(
            connection,
            ReplicationMessage.Update(
                state));
    }

    public bool SendDespawn(
        ConnectionId connection,
        NetworkEntityId networkId)
    {
        return Send(
            connection,
            ReplicationMessage.Despawn(
                networkId));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.ConnectionConnected -=
    OnConnectionReady;

        _session.ConnectionAccepted -=
            OnConnectionReady;

        _messages.Dispose();

        _messages.Dispose();

        _disposed =
            true;
    }

    private bool Send(
        ConnectionId connection,
        ReplicationMessage message)
    {
        EnsureNotDisposed();

        if (!_authority.HasAuthority)
        {
            return false;
        }

        return _messages.Send(
            connection,
            new NetworkMessage<ReplicationMessage>(
                ReplicationPacketId,
                NetworkChannel.Reliable,
                message));
    }

    private void OnMessage(
        ConnectionId connection,
        ReplicationMessage message)
    {
        if (_authority.HasAuthority)
        {
            return;
        }

        if (!_session.TryGetConnection(
                connection,
                out var networkConnection) ||
            networkConnection is null)
        {
            return;
        }

        if (networkConnection.Endpoint !=
            _authorityNode.Endpoint)
        {
            return;
        }
    
        try
        {
            switch (message.Operation)
            {
                case ReplicationOperation.Spawn:
                    _entityService.Spawn(
                        _world,
                        _entityMap,
                        message.State,
                        _context);
                    break;

                case ReplicationOperation.Update:
                    _entityService.Update(
                        _world,
                        _entityMap,
                        message.State,
                        _context);
                    break;

                case ReplicationOperation.Despawn:
                    _entityService.Despawn(
                        _world,
                        _entityMap,
                        message.State.Id);
                    break;

                default:
                    throw new InvalidDataException(
                        $"Unknown replication operation '{message.Operation}'.");
            }
        }
        catch (InvalidDataException)
        {
            // Invalid remote replication payload is ignored.
        }
        catch (InvalidOperationException)
        {
            // Invalid remote replication state is ignored.
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}