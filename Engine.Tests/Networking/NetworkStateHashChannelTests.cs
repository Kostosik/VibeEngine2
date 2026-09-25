using Engine.Core.Determinism;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Simulation;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class NetworkStateHashChannelTests
{
    [Fact]
    public void SendAndUpdate_DeliversHashForCorrectTick()
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

        serverSession.Start(
            serverEndpoint);

        clientSession.Start(
            new NetworkEndpoint(
                "client",
                1001));

        var clientConnection =
            clientSession.Connect(
                serverEndpoint);

        serverSession.Update();

        var serverConnection =
            Assert.Single(
                serverSession.Connections);

        using var clientChannel =
            new NetworkStateHashChannel(
                clientSession);

        using var serverChannel =
            new NetworkStateHashChannel(
                serverSession);

        var tick =
            new Tick(42);

        var hash =
            new DeterministicStateHash(
                123456789UL);

        Assert.True(
            clientChannel.Send(
                clientConnection.Id,
                tick,
                hash));

        serverChannel.Update();

        Assert.True(
            serverChannel.TryGet(
                serverConnection.Id,
                tick,
                out var receivedHash));

        Assert.Equal(
            hash,
            receivedHash);
    }

    [Fact]
    public void TryGet_WithUnknownTick_ReturnsFalse()
    {
        using var transport =
            new LoopbackTransport();

        using var session =
            new NetworkSession(
                transport);

        session.Start(
            new NetworkEndpoint(
                "loopback",
                1000));

        var connection =
            session.Connect(
                new NetworkEndpoint(
                    "remote",
                    1001));

        using var channel =
            new NetworkStateHashChannel(
                session);

        var found =
            channel.TryGet(
                connection.Id,
                new Tick(42),
                out _);

        Assert.False(
            found);
    }
}