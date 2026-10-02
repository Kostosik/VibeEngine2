using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class ReliableNetworkTransportTests
{
    [Fact]
    public void ReliablePacket_IsDeliveredThroughTransport()
    {
        using var server =
            new ReliableNetworkTransport(
                new LoopbackTransport());

        using var client =
            new ReliableNetworkTransport(
                new LoopbackTransport());

        using var serverSession =
            new NetworkSession(
                server);

        using var clientSession =
            new NetworkSession(
                client);

        var serverEndpoint =
            new NetworkEndpoint(
                "server",
                3100);

        var clientEndpoint =
            new NetworkEndpoint(
                "client",
                3101);

        serverSession.Start(
            serverEndpoint);

        clientSession.Start(
            clientEndpoint);

        var clientConnection =
            clientSession.Connect(
                serverEndpoint);

        serverSession.Update();

        var serverConnection =
            Assert.Single(
                serverSession.Connections);

        NetworkPacket? received =
            null;

        serverSession.PacketReceived +=
            (_, packet) =>
            {
                received =
                    packet;
            };

        var packet =
            new NetworkPacket(
                new PacketId(42),
                NetworkChannel.Reliable,
                new byte[]
                {
                    1,
                    2,
                    3,
                    4
                });

        Assert.True(
            clientSession.Send(
                clientConnection.Id,
                packet));

        serverSession.Update();

        Assert.True(
            received.HasValue);

        Assert.Equal(
            packet.Id,
            received.Value.Id);

        Assert.Equal(
            packet.Channel,
            received.Value.Channel);

        Assert.Equal(
            packet.Payload.ToArray(),
            received.Value.Payload.ToArray());

        Assert.Equal(
            serverConnection.Id,
            serverSession.Connections.Single().Id);
    }

    [Fact]
    public void Capabilities_SupportReliableAndUnreliable()
    {
        using var transport =
            new ReliableNetworkTransport(
                new LoopbackTransport());

        Assert.Equal(
            NetworkTransportCapabilities.Reliable |
            NetworkTransportCapabilities.Unreliable,
            transport.Capabilities);
    }
}