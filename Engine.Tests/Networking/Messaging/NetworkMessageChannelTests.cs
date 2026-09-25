using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Messaging;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;
using Engine.Serialization.Binary;
using Engine.Serialization.Types;

namespace Engine.Tests.Networking.Messaging;

public sealed class NetworkMessageChannelTests
{
    [Fact]
    public void SendAndReceive_DeliversTypedMessage()
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

        var messageId =
            new PacketId(100);

        using var clientChannel =
            new NetworkMessageChannel(
                clientSession,
                SerializationContext.Default);

        using var serverChannel =
            new NetworkMessageChannel(
                serverSession,
                SerializationContext.Default);

        Tick? received =
            null;

        ConnectionId receivedConnection =
            ConnectionId.Invalid;

        var serializer =
            new TickSerializer();

        clientChannel.Register(
            messageId,
            serializer,
            (_, _) => { });

        serverChannel.Register(
            messageId,
            serializer,
            (connection, message) =>
            {
                receivedConnection =
                    connection;

                received =
                    message;
            });

        var message =
            new NetworkMessage<Tick>(
                messageId,
                NetworkChannel.Reliable,
                new Tick(42));

        Assert.True(
            clientChannel.Send(
                clientConnection.Id,
                message));

        serverSession.Update();

        Assert.True(
            received.HasValue);

        Assert.Equal(
            new Tick(42),
            received.Value);

        Assert.Equal(
            serverConnection.Id,
            receivedConnection);
    }
}