using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking.Topology;

public sealed class NetworkTopologyConnectorTests
{
    [Fact]
    public void Update_TracksInboundConnectionFromTopologyPeer()
    {
        using var serverTransport =
            new LoopbackTransport();

        using var clientTransport =
            new LoopbackTransport();

        using var serverSession =
            new NetworkSession(
                serverTransport);

        using var clientSession =
            new NetworkSession(
                clientTransport);

        var serverNode =
            CreateNode(
                1,
                1101);

        var clientNode =
            CreateNode(
                2,
                1102);

        serverSession.Start(
            serverNode.Endpoint);

        clientSession.Start(
            clientNode.Endpoint);

        var connector =
            new NetworkTopologyConnector(
                serverSession,
                serverNode,
                new[]
                {
                serverNode,
                clientNode
                });

        var plan =
            new StarTopology(
                serverNode.Id)
            .Build(
                new[]
                {
                serverNode,
                clientNode
                });

        connector.Apply(
            plan);

        Assert.Empty(
            connector.Connections);

        clientSession.Connect(
            serverNode.Endpoint);

        serverSession.Update();

        connector.Update();

        var connection =
            Assert.Single(
                connector.Connections);

        Assert.Equal(
            clientNode.Id,
            connection.Key);

        Assert.Equal(
            NetworkConnectionDirection.Inbound,
            connection.Value.Direction);
    }

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
            new FullMeshTopology();

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

        var exposed =
    connector.Connections;

        if (exposed is IDictionary<
                NetworkNodeId,
                NetworkConnection> mutable)
        {
            mutable.Clear();
        }

        Assert.Equal(
            2,
            connector.Connections.Count);

        Assert.Equal(
            2,
            localSession.Connections.Count);
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