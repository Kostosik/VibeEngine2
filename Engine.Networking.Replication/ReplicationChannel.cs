using Engine.ECS;
using Engine.Networking.Connections;
using Engine.Networking.Messaging;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
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

    private bool _disposed;

    public ReplicationChannel(
        NetworkSession session,
        World world,
        NetworkEntityMap entityMap,
        ReplicationEntityService entityService,
        SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityMap);
        ArgumentNullException.ThrowIfNull(entityService);

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

        _messages.Dispose();

        _disposed =
            true;
    }

    private bool Send(
        ConnectionId connection,
        ReplicationMessage message)
    {
        EnsureNotDisposed();

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

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}