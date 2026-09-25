using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Serialization.Binary;

namespace Engine.Networking.Messaging;

public sealed class NetworkMessageChannel :
    IDisposable
{
    private readonly NetworkSession _session;

    private readonly SerializationContext _context;

    private readonly Dictionary<
        PacketId,
        Registration> _registrations =
        new();

    private bool _disposed;

    public NetworkMessageChannel(
        NetworkSession session,
        SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(
            session);

        _session =
            session;

        _context =
            context;

        _session.PacketReceived +=
            OnPacketReceived;
    }

    public void Register<T>(
        PacketId id,
        IBinarySerializer<T> serializer,
        Action<ConnectionId, T> handler)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            serializer);

        ArgumentNullException.ThrowIfNull(
            handler);

        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Message ID must be valid.",
                nameof(id));
        }

        if (_registrations.ContainsKey(id))
        {
            throw new InvalidOperationException(
                $"Message ID '{id.Value}' is already registered.");
        }

        var messageSerializer =
            new NetworkMessageSerializer<T>(
                id,
                serializer);

        _registrations.Add(
            id,
            new Registration<T>(
                messageSerializer,
                handler));
    }

    public bool Unregister(
        PacketId id)
    {
        EnsureNotDisposed();

        return _registrations.Remove(
            id);
    }

    public bool Send<T>(
        ConnectionId connection,
        NetworkMessage<T> message)
    {
        EnsureNotDisposed();

        if (!_registrations.TryGetValue(
                message.Id,
                out var registration))
        {
            return false;
        }

        if (registration.MessageType !=
            typeof(T))
        {
            throw new InvalidOperationException(
                $"Message ID '{message.Id.Value}' is registered for type '{registration.MessageType.FullName}', not '{typeof(T).FullName}'.");
        }

        var payload =
            registration.Serializer.Serialize(
                message.Payload!,
                _context);

        var packet =
            new NetworkPacket(
                message.Id,
                message.Channel,
                payload);

        return _session.Send(
            connection,
            packet);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.PacketReceived -=
            OnPacketReceived;

        _registrations.Clear();

        _disposed =
            true;
    }

    private void OnPacketReceived(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (_disposed)
        {
            return;
        }

        if (!_registrations.TryGetValue(
                packet.Id,
                out var registration))
        {
            return;
        }

        var message =
            registration.Serializer.Deserialize(
                packet.Payload.Span,
                _context);

        registration.Invoke(
            connection,
            message);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private abstract class Registration
    {
        public abstract Type MessageType { get; }

        public abstract INetworkMessageSerializer Serializer { get; }

        public abstract void Invoke(
            ConnectionId connection,
            object message);
    }

    private sealed class Registration<T> :
        Registration
    {
        private readonly Action<
            ConnectionId,
            T> _handler;

        public Registration(
            NetworkMessageSerializer<T> serializer,
            Action<ConnectionId, T> handler)
        {
            Serializer =
                serializer;

            _handler =
                handler;
        }

        public override Type MessageType =>
            typeof(T);

        public override INetworkMessageSerializer Serializer { get; }

        public override void Invoke(
            ConnectionId connection,
            object message)
        {
            if (message is not T value)
            {
                throw new InvalidDataException(
                    $"Deserialized message has unexpected type '{message.GetType().FullName}'.");
            }

            _handler(
                connection,
                value);
        }
    }
}