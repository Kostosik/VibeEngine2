using Engine.Networking.Architecture;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking.Architecture;

public sealed class P2PNetworkTests
{
    [Fact]
    public void Create_StartsFullMeshNetwork()
    {
        using var firstTransport =
            new LoopbackTransport();

        using var secondTransport =
            new LoopbackTransport();

        using var thirdTransport =
            new LoopbackTransport();

        using var firstSession =
            new NetworkSession(
                firstTransport);

        using var secondSession =
            new NetworkSession(
                secondTransport);

        using var thirdSession =
            new NetworkSession(
                thirdTransport);

        var firstNode =
            CreateNode(
                1,
                3401);

        var secondNode =
            CreateNode(
                2,
                3402);

        var thirdNode =
            CreateNode(
                3,
                3403);

        var nodes =
            new[]
            {
                firstNode,
                secondNode,
                thirdNode
            };

        var firstNetwork =
            P2PNetwork.Create(
                firstSession,
                firstNode,
                nodes,
                new FixedNetworkAuthority(
                    firstNode.Id,
                    firstNode.Id));

        var secondNetwork =
            P2PNetwork.Create(
                secondSession,
                secondNode,
                nodes,
                new FixedNetworkAuthority(
                    secondNode.Id,
                    firstNode.Id));

        var thirdNetwork =
            P2PNetwork.Create(
                thirdSession,
                thirdNode,
                nodes,
                new FixedNetworkAuthority(
                    thirdNode.Id,
                    firstNode.Id));

        firstNetwork.Start();
        secondNetwork.Start();
        thirdNetwork.Start();

        firstNetwork.Update();
        secondNetwork.Update();
        thirdNetwork.Update();

        Assert.Equal(
            2,
            firstNetwork.Connections.Count);

        Assert.Equal(
            2,
            secondNetwork.Connections.Count);

        Assert.Equal(
            2,
            thirdNetwork.Connections.Count);
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