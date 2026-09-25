namespace Engine.Networking.Topology;

public interface INetworkTopology
{
    NetworkTopologyPlan Build(
        IReadOnlyList<NetworkNode> nodes);
}