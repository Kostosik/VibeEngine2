using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking.Topology;

public sealed class NetworkTopologyConnectorTests
{
    [Fact]
    public void Apply_ConnectsLocalNodeToAllTopologyPeers()
    {
        using var localTransport =
            new LoopbackTransport();

        using var firstRemoteTransport =
            new LoopbackTransport();

        using var secondRemoteTransport =
            new LoopbackTransport();

        using var localSession =
            new NetworkSession(
                localTransport);

        using var firstRemoteSession =
            new NetworkSession(
                firstRemoteTransport);

        using var secondRemoteSession =
            new NetworkSession(
                secondRemoteTransport);

        var localNode =
            CreateNode(
                1,
                1001);

        var firstRemoteNode =
            CreateNode(
                2,
                1002);

        var secondRemoteNode =
            CreateNode(
                3,
                1003);

        localSession.Start(
            localNode.Endpoint);

        firstRemoteSession.Start(
            firstRemoteNode.Endpoint);

        secondRemoteSession.Start(
            secondRemoteNode.Endpoint);

        var topology =
            new StarTopology(
                localNode.Id);

        var plan =
            topology.Build(
                new[]
                {
                    localNode,
                    firstRemoteNode,
                    secondRemoteNode
                });

        var connector =
            new NetworkTopologyConnector(
                localSession,
                localNode,
                new[]
                {
                    localNode,
                    firstRemoteNode,
                    secondRemoteNode
                });

        connector.Apply(
            plan);

        Assert.Equal(
            2,
            connector.Connections.Count);

        Assert.Equal(
            2,
            localSession.Connections.Count);

        firstRemoteSession.Update();

        secondRemoteSession.Update();

        Assert.Single(
            firstRemoteSession.Connections);

        Assert.Single(
            secondRemoteSession.Connections);
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