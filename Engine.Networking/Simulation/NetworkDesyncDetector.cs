using Engine.Core.Determinism;
using Engine.Core.Time;
using Engine.Networking.Connections;

namespace Engine.Networking.Simulation;

public sealed class NetworkDesyncDetector
{
    private readonly NetworkStateHashChannel _hashChannel;

    public NetworkDesyncDetector(
        NetworkStateHashChannel hashChannel)
    {
        ArgumentNullException.ThrowIfNull(hashChannel);

        _hashChannel = hashChannel;
    }

    public NetworkHashComparisonResult Check(
        ConnectionId connection,
        Tick tick,
        DeterministicStateHash localHash,
        out DeterministicStateHash remoteHash)
    {
        if (!_hashChannel.TryGet(
                connection,
                tick,
                out remoteHash))
        {
            remoteHash =
                DeterministicStateHash.Empty;

            return NetworkHashComparisonResult.NotAvailable;
        }

        _hashChannel.Remove(
            connection,
            tick);

        return remoteHash == localHash
            ? NetworkHashComparisonResult.Match
            : NetworkHashComparisonResult.Mismatch;
    }
}