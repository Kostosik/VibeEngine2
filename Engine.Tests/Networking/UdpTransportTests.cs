using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class UdpTransportTests
{
    [Fact]
    public void Connect_SendAndReceive_UsesRealUdpSocket()
    {
        using var server =
            new UdpTransport();

        using var client =
            new UdpTransport();

        var serverEndpoint =
            new NetworkEndpoint(
                "127.0.0.1",
                29100);

        var clientEndpoint =
            new NetworkEndpoint(
                "127.0.0.1",
                29101);

        server.Start(
            serverEndpoint);

        client.Start(
            clientEndpoint);

        var clientConnection =
            client.Connect(
                serverEndpoint);

        var accepted =
            WaitForAccept(
                server);

        var packet =
            new NetworkPacket(
                new PacketId(1),
                NetworkChannel.Unreliable,
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

        var received =
            WaitForPacket(
                server);

        Assert.Equal(
            packet.Id,
            received.Packet.Id);

        Assert.Equal(
            packet.Channel,
            received.Packet.Channel);

        Assert.Equal(
            packet.Payload.ToArray(),
            received.Packet.Payload.ToArray());

        client.Disconnect(
            clientConnection);

        AssertEventually(
            () =>
                server.TryReceiveDisconnect(
                    out var connection) &&
                connection ==
                received.Connection);
    }

    [Fact]
    public void Capabilities_SupportOnlyUnreliable()
    {
        using var transport =
            new UdpTransport();

        Assert.Equal(
            NetworkTransportCapabilities.Unreliable,
            transport.Capabilities);
    }

    private static (
        ConnectionId Connection,
        NetworkEndpoint Endpoint) WaitForAccept(
        UdpTransport transport)
    {
        for (var i = 0; i < 100; i++)
        {
            if (transport.TryAccept(
                    out var connection,
                    out var endpoint))
            {
                return (
                    connection,
                    endpoint);
            }

            Thread.Sleep(2);
        }

        throw new Xunit.Sdk.XunitException(
            "Timed out waiting for UDP connection.");
    }

    private static (
        ConnectionId Connection,
        NetworkPacket Packet) WaitForPacket(
        UdpTransport transport)
    {
        for (var i = 0; i < 100; i++)
        {
            if (transport.TryReceive(
                    out var connection,
                    out var packet))
            {
                return (
                    connection,
                    packet);
            }

            Thread.Sleep(2);
        }

        throw new Xunit.Sdk.XunitException(
            "Timed out waiting for UDP packet.");
    }

    private static void AssertEventually(
        Func<bool> predicate)
    {
        for (var i = 0; i < 100; i++)
        {
            if (predicate())
            {
                return;
            }

            Thread.Sleep(2);
        }

        throw new Xunit.Sdk.XunitException(
            "Condition was not satisfied before timeout.");
    }
}