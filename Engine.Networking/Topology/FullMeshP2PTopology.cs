namespace Engine.Networking.Topology;

public sealed class FullMeshP2PTopology :
    INetworkTopology
{
    private readonly FullMeshTopology _topology =
        new();

    public NetworkTopologyPlan Build(
        IReadOnlyList<NetworkNode> nodes)
    {
        return _topology.Build(
            nodes);
    }
}