using Engine.Networking.Architecture;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking.Architecture;

public sealed class NetworkArchitectureTests
{
    [Fact]
    public void StarArchitecture_ClientsInitiateAndServerAccepts()
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

        var server =
            CreateNode(
                1,
                1001);

        var firstClient =
            CreateNode(
                2,
                1002);

        var secondClient =
            CreateNode(
                3,
                1003);

        var nodes =
            new[]
            {
                server,
                firstClient,
                secondClient
            };

        var serverArchitecture =
            new NetworkArchitecture(
                serverSession,
                new NetworkConfiguration(
                    server,
                    nodes,
                    new StarTopology(
                        server.Id),
                    new FixedNetworkAuthority(
                        server.Id,
                        server.Id)));

        var firstClientArchitecture =
            new NetworkArchitecture(
                firstClientSession,
                new NetworkConfiguration(
                    firstClient,
                    nodes,
                    new StarTopology(
                        server.Id),
                    new FixedNetworkAuthority(
                        firstClient.Id,
                        server.Id)));

        var secondClientArchitecture =
            new NetworkArchitecture(
                secondClientSession,
                new NetworkConfiguration(
                    secondClient,
                    nodes,
                    new StarTopology(
                        server.Id),
                    new FixedNetworkAuthority(
                        secondClient.Id,
                        server.Id)));

        serverArchitecture.Start();

        firstClientArchitecture.Start();

        secondClientArchitecture.Start();

        serverArchitecture.Update();

        Assert.Equal(
            2,
            serverArchitecture.Connections.Count);

        Assert.Single(
            firstClientArchitecture.Connections);

        Assert.Single(
            secondClientArchitecture.Connections);
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