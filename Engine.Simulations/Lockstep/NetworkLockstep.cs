using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Simulations;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Simulation;

namespace Engine.Simulations.Lockstep;

public sealed class NetworkLockstep :
    IDisposable
{
    private const int HashHistoryCapacity = 32;

    private readonly LockstepCoordinator _coordinator;
    private readonly NetworkSession _session;
    private readonly NetworkCommandChannel _channel;
    private readonly NetworkStateHashChannel _hashChannel;
    private readonly NetworkDesyncDetector _desyncDetector;

    private readonly int _localParticipant;

    private readonly Dictionary<
        int,
        ConnectionId> _connections =
        new();

    private readonly Dictionary<
        Tick,
        DeterministicStateHash> _localHashes =
        new();

    private bool _disposed;

    public NetworkLockstep(
        LockstepCoordinator coordinator,
        NetworkSession session,
        NetworkCommandChannel channel,
        NetworkStateHashChannel hashChannel,
        int localParticipant)
    {
        ArgumentNullException.ThrowIfNull(
            coordinator);

        ArgumentNullException.ThrowIfNull(
            session);

        ArgumentNullException.ThrowIfNull(
            channel);

        ArgumentNullException.ThrowIfNull(
            hashChannel);

        if (localParticipant < 0 ||
            localParticipant >= coordinator.ParticipantCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(localParticipant));
        }

        _coordinator =
            coordinator;

        _session =
            session;

        _channel =
            channel;

        _hashChannel =
            hashChannel;

        _desyncDetector =
            new NetworkDesyncDetector(
                hashChannel);

        _localParticipant =
            localParticipant;

        _session.ConnectionDisconnected +=
            OnConnectionDisconnected;
    }

    public int LocalParticipant =>
        _localParticipant;

    public event Action<
        ConnectionId,
        Tick,
        DeterministicStateHash,
        DeterministicStateHash>?
        DesyncDetected;

    public event Action<
        ConnectionId>?
        ConnectionDisconnected;

    private void EnsureNextTick(
    Tick tick)
    {
        var expectedTick =
            _coordinator.LastExecutedTick + 1;

        if (tick != expectedTick)
        {
            throw new InvalidOperationException(
                $"Expected tick '{expectedTick.Value}', " +
                $"but received tick '{tick.Value}'.");
        }
    }

    public void AddConnection(
        int participant,
        ConnectionId connection)
    {
        EnsureNotDisposed();

        if (participant < 0 ||
            participant >=
            _coordinator.ParticipantCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(participant));
        }

        if (participant ==
            _localParticipant)
        {
            throw new ArgumentException(
                "Local participant cannot have a network connection.",
                nameof(participant));
        }

        if (!connection.IsValid)
        {
            throw new ArgumentException(
                "Connection ID must be valid.",
                nameof(connection));
        }

        if (!_session.TryGetConnection(
        connection,
        out var networkConnection) ||
    networkConnection is null ||
    networkConnection.State != NetworkConnectionState.Connected)
        {
            throw new ArgumentException(
                "Connection must belong to the NetworkSession and be connected.",
                nameof(connection));
        }

        if (_connections.TryGetValue(
                participant,
                out _))
        {
            throw new InvalidOperationException(
                $"Participant '{participant}' already has a connection.");
        }

        foreach (var pair in
                 _connections)
        {
            if (pair.Value ==
                connection)
            {
                throw new InvalidOperationException(
                    $"Connection '{connection.Value}' is already assigned to participant '{pair.Key}'.");
            }
        }

        _connections.Add(
            participant,
            connection);
    }

    public bool RemoveConnection(
        int participant)
    {
        EnsureNotDisposed();

        return _connections.Remove(
            participant);
    }

    public bool TryGetConnection(
        int participant,
        out ConnectionId connection)
    {
        EnsureNotDisposed();

        if (_connections.TryGetValue(
                participant,
                out connection))
        {
            return true;
        }

        connection =
            ConnectionId.Invalid;

        return false;
    }

    public void UpdateNetwork()
    {
        EnsureNotDisposed();

        _session.Update();
    }

    public bool IsReady(
        Tick tick)
    {
        EnsureNotDisposed();

        return _coordinator.IsReady(
            tick);
    }

    public bool TryPrepareTick(
        Tick tick)
    {
        EnsureNotDisposed();

        EnsureNextTick(
    tick);

        foreach (var pair in
                 _connections)
        {
            var participant =
                pair.Key;

            var connection =
                pair.Value;

            _channel.RemoveBefore(
                connection,
                tick);

            if (!_channel.TryGet(
                    connection,
                    tick,
                    out var commands))
            {
                continue;
            }

            _coordinator.Submit(
                tick,
                participant,
                commands);

            _channel.Remove(
                connection,
                tick);
        }

        return _coordinator.IsReady(
            tick);
    }

    public void SubmitLocal(
        Tick tick,
        IReadOnlyList<ICommand> commands)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            commands);

        _coordinator.Submit(
            tick,
            _localParticipant,
            commands);

        var batch =
            new NetworkCommandBatch(
                tick);

        foreach (var command in
                 commands)
        {
            batch.Add(
                command);
        }

        foreach (var connection in
                 _connections.Values)
        {
            if (!_channel.Send(
                    connection,
                    batch))
            {
                throw new InvalidOperationException(
                    $"Failed to send commands for tick '{tick}'.");
            }
        }
    }

    public bool TryExecute(
        Tick tick,
        FixedSystemContext context)
    {
        EnsureNotDisposed();

        EnsureNextTick(
    tick);

        UpdateNetwork();

        CheckRemoteHashes();

        if (!TryPrepareTick(
                tick))
        {
            return false;
        }

        var executed =
            _coordinator.TryExecute(
                tick,
                context);

        if (!executed)
        {
            return false;
        }

        var hash =
            _coordinator.GetStateHash();

        _localHashes[tick] =
            hash;

        TrimHashHistory();

        foreach (var connection in
                 _connections.Values)
        {
            if (!_hashChannel.Send(
                    connection,
                    tick,
                    hash))
            {
                throw new InvalidOperationException(
                    $"Failed to send state hash for tick '{tick}'.");
            }
        }

        CheckRemoteHashes();

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.ConnectionDisconnected -=
            OnConnectionDisconnected;

        _localHashes.Clear();

        _connections.Clear();

        _disposed =
            true;
    }

    private void OnConnectionDisconnected(
        NetworkConnection connection)
    {
        if (_disposed)
        {
            return;
        }

        var participant =
            -1;

        foreach (var pair in
                 _connections)
        {
            if (pair.Value !=
                connection.Id)
            {
                continue;
            }

            participant =
                pair.Key;

            break;
        }

        if (participant < 0)
        {
            return;
        }

        _connections.Remove(
            participant);

        ConnectionDisconnected?.Invoke(
            connection.Id);
    }

    private void CheckRemoteHashes()
    {
        if (_localHashes.Count == 0)
        {
            return;
        }

        var oldestLocalTick =
            _localHashes.Keys.Min();

        foreach (var pair in
                 _connections)
        {
            var connection =
                pair.Value;

            _hashChannel.RemoveBefore(
                connection,
                oldestLocalTick);

            foreach (var localHash in
                     _localHashes)
            {
                var result =
                    _desyncDetector.Check(
                        connection,
                        localHash.Key,
                        localHash.Value,
                        out var remoteHash);

                if (result !=
                    NetworkHashComparisonResult.Mismatch)
                {
                    continue;
                }

                DesyncDetected?.Invoke(
                    connection,
                    localHash.Key,
                    localHash.Value,
                    remoteHash);
            }
        }
    }

    private void TrimHashHistory()
    {
        while (_localHashes.Count >
               HashHistoryCapacity)
        {
            var oldestTick =
                _localHashes.Keys
                    .OrderBy(
                        static tick => tick)
                    .First();

            _localHashes.Remove(
                oldestTick);
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}