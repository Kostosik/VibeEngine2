using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class LoopbackTransportTests
{
    [Fact]
    public void Connect_FromClient_IsAcceptedByServer()
    {
        using var server =
            new LoopbackTransport();

        using var client =
            new LoopbackTransport();

        var serverEndpoint =
            new NetworkEndpoint(
                "server",
                1000);

        var clientEndpoint =
            new NetworkEndpoint(
                "client",
                1001);

        server.Start(
            serverEndpoint);

        client.Start(
            clientEndpoint);

        var clientConnection =
            client.Connect(
                serverEndpoint);

        Assert.True(
            server.TryAccept(
                out var serverConnection,
                out var remoteEndpoint));

        Assert.Equal(
            clientEndpoint,
            remoteEndpoint);

        var packet =
            new NetworkPacket(
                new PacketId(1),
                NetworkChannel.Reliable,
                new byte[]
                {
                    10,
                    20,
                    30
                });

        Assert.True(
            client.Send(
                clientConnection,
                packet));

        Assert.True(
            server.TryReceive(
                out var receivedConnection,
                out var received));

        Assert.Equal(
            serverConnection,
            receivedConnection);

        Assert.Equal(
            packet.Id,
            received.Id);

        Assert.Equal(
            packet.Channel,
            received.Channel);

        Assert.Equal(
            packet.Payload.ToArray(),
            received.Payload.ToArray());
    }
}