using Engine.Core.Determinism;
using Engine.Core.Time;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Simulation;
using Engine.Networking.Transport;

namespace Engine.Tests.Networking;

public sealed class NetworkDesyncDetectorTests
{
    [Fact]
    public void Check_WhenHashIsNotAvailable_ReturnsNotAvailable()
    {
        using var clientTransport =
            new LoopbackTransport();

        using var serverTransport =
            new LoopbackTransport();

        using var clientSession =
            new NetworkSession(
                clientTransport);

        using var serverSession =
            new NetworkSession(
                serverTransport);

        CreateConnectedSessions(
            clientSession,
            serverSession,
            out var clientConnection,
            out _);

        using var hashChannel =
            new NetworkStateHashChannel(
                clientSession);

        var detector =
            new NetworkDesyncDetector(
                hashChannel);

        var tick =
            TickFromInt(1);

        var localHash =
            CreateHash(10);

        var result =
            detector.Check(
                clientConnection.Id,
                tick,
                localHash,
                out var remoteHash);

        Assert.Equal(
            NetworkHashComparisonResult.NotAvailable,
            result);

        Assert.Equal(
            DeterministicStateHash.Empty,
            remoteHash);
    }

    [Fact]
    public void Check_WhenHashesMatch_ReturnsMatchAndConsumesRemoteHash()
    {
        using var clientTransport =
            new LoopbackTransport();

        using var serverTransport =
            new LoopbackTransport();

        using var clientSession =
            new NetworkSession(
                clientTransport);

        using var serverSession =
            new NetworkSession(
                serverTransport);

        CreateConnectedSessions(
            clientSession,
            serverSession,
            out var clientConnection,
            out var serverConnection);

        using var clientHashChannel =
            new NetworkStateHashChannel(
                clientSession);

        using var serverHashChannel =
            new NetworkStateHashChannel(
                serverSession);

        var detector =
            new NetworkDesyncDetector(
                clientHashChannel);

        var tick =
            TickFromInt(1);

        var hash =
            CreateHash(10);

        Assert.True(
            serverHashChannel.Send(
                serverConnection.Id,
                tick,
                hash));

        clientSession.Update();

        var result =
            detector.Check(
                clientConnection.Id,
                tick,
                hash,
                out var remoteHash);

        Assert.Equal(
            NetworkHashComparisonResult.Match,
            result);

        Assert.Equal(
            hash,
            remoteHash);

        Assert.False(
            clientHashChannel.TryGet(
                clientConnection.Id,
                tick,
                out _));
    }

    [Fact]
    public void Check_WhenHashesDiffer_ReturnsMismatchAndRemoteHash()
    {
        using var clientTransport =
            new LoopbackTransport();

        using var serverTransport =
            new LoopbackTransport();

        using var clientSession =
            new NetworkSession(
                clientTransport);

        using var serverSession =
            new NetworkSession(
                serverTransport);

        CreateConnectedSessions(
            clientSession,
            serverSession,
            out var clientConnection,
            out var serverConnection);

        using var clientHashChannel =
            new NetworkStateHashChannel(
                clientSession);

        using var serverHashChannel =
            new NetworkStateHashChannel(
                serverSession);

        var detector =
            new NetworkDesyncDetector(
                clientHashChannel);

        var tick =
            TickFromInt(1);

        var localHash =
            CreateHash(10);

        var remoteHash =
            CreateHash(20);

        Assert.NotEqual(
            localHash,
            remoteHash);

        Assert.True(
            serverHashChannel.Send(
                serverConnection.Id,
                tick,
                remoteHash));

        clientSession.Update();

        var result =
            detector.Check(
                clientConnection.Id,
                tick,
                localHash,
                out var receivedHash);

        Assert.Equal(
            NetworkHashComparisonResult.Mismatch,
            result);

        Assert.Equal(
            remoteHash,
            receivedHash);

        Assert.False(
            clientHashChannel.TryGet(
                clientConnection.Id,
                tick,
                out _));
    }

    [Fact]
    public void Check_WhenHashIsNotAvailable_DoesNotConsumeFutureHash()
    {
        using var clientTransport =
            new LoopbackTransport();

        using var serverTransport =
            new LoopbackTransport();

        using var clientSession =
            new NetworkSession(
                clientTransport);

        using var serverSession =
            new NetworkSession(
                serverTransport);

        CreateConnectedSessions(
            clientSession,
            serverSession,
            out var clientConnection,
            out var serverConnection);

        using var clientHashChannel =
            new NetworkStateHashChannel(
                clientSession);

        using var serverHashChannel =
            new NetworkStateHashChannel(
                serverSession);

        var detector =
            new NetworkDesyncDetector(
                clientHashChannel);

        var tick =
            TickFromInt(1);

        var localHash =
            CreateHash(10);

        var firstResult =
            detector.Check(
                clientConnection.Id,
                tick,
                localHash,
                out var firstRemoteHash);

        Assert.Equal(
            NetworkHashComparisonResult.NotAvailable,
            firstResult);

        Assert.Equal(
            DeterministicStateHash.Empty,
            firstRemoteHash);

        var remoteHash =
            CreateHash(20);

        Assert.True(
            serverHashChannel.Send(
                serverConnection.Id,
                tick,
                remoteHash));

        clientSession.Update();

        var secondResult =
            detector.Check(
                clientConnection.Id,
                tick,
                localHash,
                out var receivedHash);

        Assert.Equal(
            NetworkHashComparisonResult.Mismatch,
            secondResult);

        Assert.Equal(
            remoteHash,
            receivedHash);
    }

    private static void CreateConnectedSessions(
        NetworkSession clientSession,
        NetworkSession serverSession,
        out NetworkConnection clientConnection,
        out NetworkConnection serverConnection)
    {
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

        clientConnection =
            clientSession.Connect(
                serverEndpoint);

        serverSession.Update();

        serverConnection =
            Assert.Single(
                serverSession.Connections);
    }

    private static DeterministicStateHash CreateHash(
        int value)
    {
        var hasher =
            DeterministicStateHasher.Create();

        hasher.AddInt32(
            value);

        return hasher.GetHash();
    }

    private static Tick TickFromInt(
        int value)
    {
        var tick =
            Tick.Zero;

        for (var i = 0; i < value; i++)
        {
            tick =
                tick++;
        }

        return tick;
    }
}