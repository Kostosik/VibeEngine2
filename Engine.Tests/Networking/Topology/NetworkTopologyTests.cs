using Engine.Networking.Connections;
using Engine.Networking.Topology;

namespace Engine.Tests.Networking.Topology;

public sealed class NetworkTopologyTests
{
    [Fact]
    public void FullMesh_ConnectsEveryNodeToEveryOtherNode()
    {
        var nodes =
            new[]
            {
                CreateNode(1, 1001),
                CreateNode(2, 1002),
                CreateNode(3, 1003)
            };

        var topology =
            new FullMeshTopology();

        var plan =
            topology.Build(
                nodes);

        Assert.Equal(
            3,
            plan.Edges.Count);

        Assert.Equal(
            2,
            plan.GetPeers(
                new NetworkNodeId(1)).Count);

        Assert.Equal(
            2,
            plan.GetPeers(
                new NetworkNodeId(2)).Count);

        Assert.Equal(
            2,
            plan.GetPeers(
                new NetworkNodeId(3)).Count);
    }

    [Fact]
    public void Star_ConnectsEveryNodeToCenter()
    {
        var center =
            new NetworkNodeId(1);

        var nodes =
            new[]
            {
                CreateNode(1, 1001),
                CreateNode(2, 1002),
                CreateNode(3, 1003),
                CreateNode(4, 1004)
            };

        var topology =
            new StarTopology(
                center);

        var plan =
            topology.Build(
                nodes);

        Assert.Equal(
            3,
            plan.Edges.Count);

        Assert.Equal(
            3,
            plan.GetPeers(
                center).Count);

        Assert.Single(
            plan.GetPeers(
                new NetworkNodeId(2)));

        Assert.Single(
            plan.GetPeers(
                new NetworkNodeId(3)));

        Assert.Single(
            plan.GetPeers(
                new NetworkNodeId(4)));
    }

    private static NetworkNode CreateNode(
        ulong id,
        int port)
    {
        return new NetworkNode(
            new NetworkNodeId(id),
            new NetworkEndpoint(
                $"node-{id}",
                port));
    }
}