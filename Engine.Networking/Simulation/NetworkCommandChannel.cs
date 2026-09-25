using Engine.Core.Commands;
using Engine.Core.Replays;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;

namespace Engine.Networking.Simulation;

public sealed class NetworkCommandChannel : IDisposable
{
    private static readonly PacketId CommandBatchPacketId =
        new(1);

    private readonly NetworkSession _session;
    private readonly ReplayCommandRegistry _registry;

    private readonly Dictionary<
        ConnectionId,
        Dictionary<Tick, IReadOnlyList<ICommand>>> _received =
        new();

    private bool _disposed;

    public NetworkCommandChannel(
        NetworkSession session,
        ReplayCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(registry);

        _session = session;
        _registry = registry;

        _session.PacketReceived +=
            OnPacketReceived;
    }

    public bool Send(
        ConnectionId connection,
        NetworkCommandBatch batch)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(batch);

        var payload =
            NetworkCommandSerializer.Serialize(
                batch,
                _registry);

        var packet =
            new NetworkPacket(
                CommandBatchPacketId,
                NetworkChannel.Reliable,
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
        out IReadOnlyList<ICommand> commands)
    {
        EnsureNotDisposed();

        if (_received.TryGetValue(
                connection,
                out var ticks) &&
            ticks.TryGetValue(
                tick,
                out commands!))
        {
            return true;
        }

        commands = Array.Empty<ICommand>();

        return false;
    }

    public bool Remove(
        ConnectionId connection,
        Tick tick)
    {
        EnsureNotDisposed();

        return _received.TryGetValue(
                   connection,
                   out var ticks) &&
               ticks.Remove(tick);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.PacketReceived -=
            OnPacketReceived;

        _received.Clear();

        _disposed = true;
    }

    private void OnPacketReceived(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (packet.Id != CommandBatchPacketId)
        {
            return;
        }

        var batch =
            NetworkCommandSerializer.Deserialize(
                packet.Payload.Span,
                _registry);

        if (!_received.TryGetValue(
                connection,
                out var ticks))
        {
            ticks =
                new Dictionary<Tick, IReadOnlyList<ICommand>>();

            _received.Add(
                connection,
                ticks);
        }

        if (ticks.ContainsKey(batch.Tick))
        {
            return;
        }

        ticks.Add(
            batch.Tick,
            batch.Commands);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}