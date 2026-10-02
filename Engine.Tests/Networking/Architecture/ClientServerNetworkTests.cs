using Engine.Networking.Architecture;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking.Architecture;

public sealed class ClientServerNetworkTests
{
    [Fact]
    public void CreateServerAndClients_BuildsExpectedNetwork()
    {
        using var serverTransport =
            new LoopbackTransport();

        using var firstClientTransport =
            new LoopbackTransport();

        using var secondClientTransport =
            new LoopbackTransport();

        using var serverSession =
            new NetworkSession(
                serverTransport);

        using var firstClientSession =
            new NetworkSession(
                firstClientTransport);

        using var secondClientSession =
            new NetworkSession(
                secondClientTransport);

        var serverNode =
            CreateNode(
                1,
                3301);

        var firstClientNode =
            CreateNode(
                2,
                3302);

        var secondClientNode =
            CreateNode(
                3,
                3303);

        var nodes =
            new[]
            {
                serverNode,
                firstClientNode,
                secondClientNode
            };

        var server =
            ClientServerNetwork.CreateServer(
                serverSession,
                serverNode,
                nodes);

        var firstClient =
            ClientServerNetwork.CreateClient(
                firstClientSession,
                firstClientNode,
                serverNode.Id,
                nodes);

        var secondClient =
            ClientServerNetwork.CreateClient(
                secondClientSession,
                secondClientNode,
                serverNode.Id,
                nodes);

        Assert.True(
            server.HasAuthority);

        Assert.False(
            firstClient.HasAuthority);

        Assert.False(
            secondClient.HasAuthority);

        Assert.Equal(
            serverNode.Id,
            firstClient.ServerNode);

        server.Start();
        firstClient.Start();
        secondClient.Start();

        server.Update();

        Assert.Equal(
            2,
            server.Connections.Count);

        Assert.Single(
            firstClient.Connections);

        Assert.Single(
            secondClient.Connections);
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