using Engine.Core.Determinism;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;

namespace Engine.Networking.Simulation;

public sealed class NetworkStateHashChannel : IDisposable
{
    private static readonly PacketId StateHashPacketId =
        new(2);

    private readonly NetworkSession _session;

    private readonly Dictionary<
        ConnectionId,
        Dictionary<Tick, DeterministicStateHash>> _received = new();

    private bool _disposed;

    public NetworkStateHashChannel(
        NetworkSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;

        _session.PacketReceived +=
            OnPacketReceived;
        _session.ConnectionDisconnected +=
    OnConnectionDisconnected;
    }

    public bool Send(
        ConnectionId connection,
        Tick tick,
        DeterministicStateHash hash)
    {
        EnsureNotDisposed();

        var message =
            new NetworkStateHashMessage(
                tick,
                hash);

        var payload =
            NetworkStateHashSerializer.Serialize(
                message);

        var packet =
            new NetworkPacket(
                StateHashPacketId,
                NetworkChannel.Unreliable,
                payload);

        return _session.Send(
            connection,
            packet);
    }

    public void Update()
    {
        EnsureNotDisposed();

        _session.Update();
    }

    public bool TryGet(
        ConnectionId connection,
        Tick tick,
        out DeterministicStateHash hash)
    {
        EnsureNotDisposed();

        if (_received.TryGetValue(
                connection,
                out var hashes) &&
            hashes.TryGetValue(
                tick,
                out hash))
        {
            return true;
        }

        hash =
            DeterministicStateHash.Empty;

        return false;
    }

    public bool Remove(
        ConnectionId connection,
        Tick tick)
    {
        EnsureNotDisposed();

        return _received.TryGetValue(
                   connection,
                   out var hashes) &&
               hashes.Remove(tick);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.ConnectionDisconnected -=
            OnConnectionDisconnected;

        _session.PacketReceived -=
            OnPacketReceived;

        _received.Clear();

        _disposed = true;
    }

    public int RemoveBefore(
    ConnectionId connection,
    Tick tick)
    {
        EnsureNotDisposed();

        if (!_received.TryGetValue(
                connection,
                out var hashes))
        {
            return 0;
        }

        var staleTicks =
            hashes.Keys
                .Where(
                    current => current.Value < tick.Value)
                .ToArray();

        foreach (var staleTick in staleTicks)
        {
            hashes.Remove(
                staleTick);
        }

        return staleTicks.Length;
    }

    private void OnConnectionDisconnected(
    NetworkConnection connection)
    {
        _received.Remove(
            connection.Id);
    }

    private void OnPacketReceived(
     ConnectionId connection,
     NetworkPacket packet)
    {
        if (packet.Id != StateHashPacketId)
        {
            return;
        }

        NetworkStateHashMessage message;

        try
        {
            message =
                NetworkStateHashSerializer.Deserialize(
                    packet.Payload.Span);
        }
        catch (InvalidDataException)
        {
            return;
        }

        if (!_received.TryGetValue(
                connection,
                out var hashes))
        {
            hashes =
                new Dictionary<
                    Tick,
                    DeterministicStateHash>();

            _received.Add(
                connection,
                hashes);
        }

        hashes[message.Tick] =
            message.Hash;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}