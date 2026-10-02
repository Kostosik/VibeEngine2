using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class ReliableUdpTransportTests
{
    [Fact]
    public void ReliablePacket_IsDeliveredThroughRealUdp()
    {
        using var server =
            new ReliableNetworkTransport(
                new UdpTransport());

        using var client =
            new ReliableNetworkTransport(
                new UdpTransport());

        using var serverSession =
            new NetworkSession(
                server);

        using var clientSession =
            new NetworkSession(
                client);

        var serverEndpoint =
            new NetworkEndpoint(
                "127.0.0.1",
                32100);

        var clientEndpoint =
            new NetworkEndpoint(
                "127.0.0.1",
                32101);

        serverSession.Start(
            serverEndpoint);

        clientSession.Start(
            clientEndpoint);

        var clientConnection =
            clientSession.Connect(
                serverEndpoint);

        var serverConnection =
            WaitForAccept(
                serverSession);

        NetworkPacket? received =
            null;

        ConnectionId receivedConnection =
            ConnectionId.Invalid;

        serverSession.PacketReceived +=
            (connection, packet) =>
            {
                receivedConnection =
                    connection;

                received =
                    packet;
            };

        var packet =
            new NetworkPacket(
                new PacketId(77),
                NetworkChannel.Reliable,
                new byte[]
                {
                    10,
                    20,
                    30,
                    40
                });

        Assert.True(
            clientSession.Send(
                clientConnection.Id,
                packet));

        WaitForPacket(
            serverSession,
            () =>
                received.HasValue);

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
            serverConnection,
            receivedConnection);
    }

    private static ConnectionId WaitForAccept(
        NetworkSession session)
    {
        for (var i = 0; i < 200; i++)
        {
            session.Update();

            var connection =
                session.Connections
                    .FirstOrDefault();

            if (connection is not null)
            {
                return connection.Id;
            }

            Thread.Sleep(2);
        }

        throw new Xunit.Sdk.XunitException(
            "Timed out waiting for UDP connection.");
    }

    private static void WaitForPacket(
        NetworkSession session,
        Func<bool> predicate)
    {
        for (var i = 0; i < 200; i++)
        {
            session.Update();

            if (predicate())
            {
                return;
            }

            Thread.Sleep(2);
        }

        throw new Xunit.Sdk.XunitException(
            "Timed out waiting for UDP packet.");
    }
}