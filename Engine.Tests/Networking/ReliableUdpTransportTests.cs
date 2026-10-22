using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;
using System.Net;
using System.Net.Sockets;

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

        var (serverEndpoint, clientEndpoint) =
            CreateEndpoints();

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

    private static (
    NetworkEndpoint Server,
    NetworkEndpoint Client) CreateEndpoints()
    {
        using var serverSocket =
            new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);

        using var clientSocket =
            new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);

        serverSocket.Bind(
            new IPEndPoint(
                IPAddress.Loopback,
                0));

        clientSocket.Bind(
            new IPEndPoint(
                IPAddress.Loopback,
                0));

        var serverPort =
            ((IPEndPoint)serverSocket.LocalEndPoint!).Port;

        var clientPort =
            ((IPEndPoint)clientSocket.LocalEndPoint!).Port;

        return (
            new NetworkEndpoint(
                "127.0.0.1",
                serverPort),

            new NetworkEndpoint(
                "127.0.0.1",
                clientPort));
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