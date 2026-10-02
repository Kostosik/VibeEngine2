using Engine.ECS;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Replication;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
using Engine.Networking.Transport;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class ReplicationChannelUdpTests
{
    [Fact]
    public void ReplicationChannel_TransfersSpawnUpdateAndDespawn_OverUdp()
    {
        using var serverTransport =
            new ReliableNetworkTransport(
                new UdpTransport());

        using var clientTransport =
            new ReliableNetworkTransport(
                new UdpTransport());

        using var serverSession =
            new NetworkSession(
                serverTransport);

        using var clientSession =
            new NetworkSession(
                clientTransport);

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
            WaitForConnection(
                serverSession);

        var serverNode =
            new NetworkNode(
                new NetworkNodeId(1),
                serverEndpoint);

        var clientNode =
            new NetworkNode(
                new NetworkNodeId(2),
                clientEndpoint);

        var serverAuthority =
            new FixedNetworkAuthority(
                serverNode.Id,
                serverNode.Id);

        var clientAuthority =
            new FixedNetworkAuthority(
                clientNode.Id,
                serverNode.Id);

        using var serverWorld =
            new World();

        using var clientWorld =
            new World();

        var serverMap =
            new NetworkEntityMap(
                serverWorld);

        var clientMap =
            new NetworkEntityMap(
                clientWorld);

        var serverRegistry =
            CreateRegistry();

        var clientRegistry =
            CreateRegistry();

        var serverStateService =
            new ReplicationStateService(
                serverRegistry);

        var clientStateService =
            new ReplicationStateService(
                clientRegistry);

        var serverEntityService =
            new ReplicationEntityService(
                serverStateService);

        var clientEntityService =
            new ReplicationEntityService(
                clientStateService);

        using var serverChannel =
            new ReplicationChannel(
                serverSession,
                serverWorld,
                serverMap,serverStateService,
                serverEntityService,
                serverAuthority,
                serverNode,
                SerializationContext.Default);

        using var clientChannel =
            new ReplicationChannel(
                clientSession,
                clientWorld,
                clientMap,clientStateService,
                clientEntityService,
                clientAuthority,
                serverNode,
                SerializationContext.Default);

        var serverEntity =
            serverWorld.CreateEntity();

        var networkId =
            new NetworkEntityId(200);

        serverMap.Register(
            serverEntity,
            networkId);

        serverWorld.Add(
            serverEntity,
            new TestComponent
            {
                Value = 10
            });

        var state =
            serverStateService.Capture(
                serverWorld,
                serverMap,
                serverEntity,
                SerializationContext.Default);

        Assert.True(
            serverChannel.SendSpawn(
                serverConnection.Id,
                state));

        WaitForReplication(
            clientSession,
            () =>
                clientMap.TryGetEntity(
                    networkId,
                    out _));

        Assert.True(
            clientMap.TryGetEntity(
                networkId,
                out var clientEntity));

        Assert.Equal(
            10,
            clientWorld.Get<TestComponent>(
                clientEntity).Value);

        serverWorld.Get<TestComponent>(
            serverEntity).Value = 20;

        state =
            serverStateService.Capture(
                serverWorld,
                serverMap,
                serverEntity,
                SerializationContext.Default);

        Assert.True(
            serverChannel.SendUpdate(
                serverConnection.Id,
                state));

        WaitForReplication(
            clientSession,
            () =>
                clientWorld.Get<TestComponent>(
                    clientEntity).Value == 20);

        Assert.Equal(
            20,
            clientWorld.Get<TestComponent>(
                clientEntity).Value);

        Assert.True(
            serverChannel.SendDespawn(
                serverConnection.Id,
                networkId));

        WaitForReplication(
            clientSession,
            () =>
                !clientWorld.Exists(
                    clientEntity));

        Assert.False(
            clientWorld.Exists(
                clientEntity));

        Assert.False(
            clientMap.TryGetEntity(
                networkId,
                out _));

        Assert.Equal(
            clientConnection.Id,
            clientSession.Connections.Single().Id);

        Assert.Equal(
            serverConnection.Id,
            serverSession.Connections.Single().Id);
    }

    private static ReplicatedComponentRegistry CreateRegistry()
    {
        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new TestComponentSerializer());

        return registry;
    }

    private static void WaitForReplication(
        NetworkSession session,
        Func<bool> condition)
    {
        for (var i = 0; i < 100; i++)
        {
            session.Update();

            if (condition())
            {
                return;
            }

            Thread.Sleep(2);
        }

        throw new Xunit.Sdk.XunitException(
            "Timed out waiting for network replication.");
    }

    private static NetworkConnection WaitForConnection(
        NetworkSession session)
    {
        for (var i = 0; i < 100; i++)
        {
            session.Update();

            if (session.Connections.Count != 0)
            {
                return session.Connections.Single();
            }

            Thread.Sleep(2);
        }

        throw new Xunit.Sdk.XunitException(
            "Timed out waiting for UDP connection.");
    }

    private struct TestComponent
    {
        public int Value { get; set; }
    }

    private sealed class TestComponentSerializer :
        IBinarySerializer<TestComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            TestComponent value)
        {
            writer.WriteInt32(
                value.Value);
        }

        public TestComponent Deserialize(
            ref SerializationReader reader)
        {
            return new TestComponent
            {
                Value =
                    reader.ReadInt32()
            };
        }
    }
}