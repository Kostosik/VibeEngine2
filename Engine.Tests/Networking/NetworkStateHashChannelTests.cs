using Engine.Core.Determinism;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Sessions;
using Engine.Networking.Simulation;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class NetworkStateHashChannelTests
{
    [Fact]
    public void RemoveBefore_RemovesOnlyOlderHashes()
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

        serverSession.Start(
            new NetworkEndpoint(
                "hash-cleanup-server",
                1940));

        clientSession.Start(
            new NetworkEndpoint(
                "hash-cleanup-client",
                1941));

        var clientConnection =
            clientSession.Connect(
                serverSession.LocalEndpoint);

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

        Assert.True(
            clientChannel.Send(
                clientConnection.Id,
                new Tick(1),
                new DeterministicStateHash(1)));

        Assert.True(
            clientChannel.Send(
                clientConnection.Id,
                new Tick(5),
                new DeterministicStateHash(5)));

        Assert.True(
            clientChannel.Send(
                clientConnection.Id,
                new Tick(10),
                new DeterministicStateHash(10)));

        serverChannel.Update();

        Assert.Equal(
            2,
            serverChannel.RemoveBefore(
                serverConnection.Id,
                new Tick(10)));

        Assert.False(
            serverChannel.TryGet(
                serverConnection.Id,
                new Tick(1),
                out _));

        Assert.False(
            serverChannel.TryGet(
                serverConnection.Id,
                new Tick(5),
                out _));

        Assert.True(
            serverChannel.TryGet(
                serverConnection.Id,
                new Tick(10),
                out var hash));

        Assert.Equal(
            new DeterministicStateHash(10),
            hash);
    }

    [Fact]
    public void Update_MalformedPacket_DoesNotBlockFollowingValidHash()
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
                "malformed-hash-server",
                1310);

        var clientEndpoint =
            new NetworkEndpoint(
                "malformed-hash-client",
                1311);

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

        using var serverChannel =
            new NetworkStateHashChannel(
                serverSession);

        Assert.True(
            clientSession.Send(
                clientConnection.Id,
                new NetworkPacket(
                    new PacketId(2),
                    NetworkChannel.Unreliable,
                    new byte[] { 1, 2, 3 })));

        var expectedHash =
            new DeterministicStateHash(
                12345UL);

        using var clientChannel =
            new NetworkStateHashChannel(
                clientSession);

        Assert.True(
            clientChannel.Send(
                clientConnection.Id,
                new Tick(42),
                expectedHash));

        serverChannel.Update();

        Assert.True(
            serverChannel.TryGet(
                serverConnection.Id,
                new Tick(42),
                out var receivedHash));

        Assert.Equal(
            expectedHash,
            receivedHash);
    }

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
                1300);

        serverSession.Start(
            serverEndpoint);

        clientSession.Start(
            new NetworkEndpoint(
                "client",
                1301));

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
                1600));

        using var channel =
            new NetworkStateHashChannel(
                session);

        var found =
            channel.TryGet(
                new ConnectionId(1),
                new Tick(42),
                out _);

        Assert.False(
            found);
    }
}