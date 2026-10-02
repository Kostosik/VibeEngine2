namespace Engine.Networking.Topology;

public sealed class ClientServerTopology :
    INetworkTopology
{
    private readonly StarTopology _topology;

    public ClientServerTopology(
        NetworkNodeId server)
    {
        _topology =
            new StarTopology(
                server);
    }

    public NetworkTopologyPlan Build(
        IReadOnlyList<NetworkNode> nodes)
    {
        return _topology.Build(
            nodes);
    }
}