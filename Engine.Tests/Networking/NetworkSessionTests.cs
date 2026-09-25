using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class NetworkSessionTests
{
    [Fact]
    public void Connect_SendAndUpdate_RaisesPacketReceived()
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

        var serverEndpoint =
            new NetworkEndpoint(
                "server",
                1000);

        var clientEndpoint =
            new NetworkEndpoint(
                "client",
                1001);

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

        NetworkPacket? received = null;

        serverSession.PacketReceived +=
            (_, packet) =>
            {
                received = packet;
            };

        var packet =
            new NetworkPacket(
                new PacketId(1),
                NetworkChannel.Reliable,
                new byte[] { 1, 2, 3 });

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
            packet.Payload.ToArray(),
            received.Value.Payload.ToArray());
    }
}