using Engine.ECS;
using Engine.Networking.Connections;
using Engine.Networking.Replication;
using Engine.Networking.Sessions;
using Engine.Networking.Transport;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class ReplicationChannelTests
{
    [Fact]
    public void ReplicationChannel_TransfersSpawnUpdateAndDespawn()
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

        var clientEndpoint =
            new NetworkEndpoint(
                "replication-client",
                1700);

        var serverEndpoint =
            new NetworkEndpoint(
                "replication-server",
                1701);

        clientSession.Start(
            clientEndpoint);

        serverSession.Start(
            serverEndpoint);

        var clientConnection =
            clientSession.Connect(
                serverEndpoint);

        serverSession.Update();

        var serverConnection =
            Assert.Single(
                serverSession.Connections);

        using var clientWorld =
            new World();

        using var serverWorld =
            new World();

        var clientMap =
            new NetworkEntityMap(
                clientWorld);

        var serverMap =
            new NetworkEntityMap(
                serverWorld);

        var clientRegistry =
            CreateRegistry();

        var serverRegistry =
            CreateRegistry();

        var clientStateService =
            new ReplicationStateService(
                clientRegistry);

        var serverStateService =
            new ReplicationStateService(
                serverRegistry);

        var clientEntityService =
            new ReplicationEntityService(
                clientStateService);

        var serverEntityService =
            new ReplicationEntityService(
                serverStateService);

        using var clientChannel =
            new ReplicationChannel(
                clientSession,
                clientWorld,
                clientMap,
                clientEntityService,
                SerializationContext.Default);

        using var serverChannel =
            new ReplicationChannel(
                serverSession,
                serverWorld,
                serverMap,
                serverEntityService,
                SerializationContext.Default);

        var clientEntity =
            clientWorld.CreateEntity();

        var networkId =
            new NetworkEntityId(100);

        clientMap.Register(
            clientEntity,
            networkId);

        clientWorld.Add(
            clientEntity,
            new TestComponent
            {
                Value = 10
            });

        var state =
            clientStateService.Capture(
                clientWorld,
                clientMap,
                clientEntity,
                SerializationContext.Default);

        Assert.True(
            clientChannel.SendSpawn(
                clientConnection.Id,
                state));

        serverSession.Update();

        Assert.True(
            serverMap.TryGetEntity(
                networkId,
                out var serverEntity));

        Assert.True(
            serverWorld.Exists(
                serverEntity));

        Assert.Equal(
            10,
            serverWorld.Get<TestComponent>(
                serverEntity).Value);

        clientWorld.Get<TestComponent>(
            clientEntity).Value = 20;

        state =
            clientStateService.Capture(
                clientWorld,
                clientMap,
                clientEntity,
                SerializationContext.Default);

        Assert.True(
            clientChannel.SendUpdate(
                clientConnection.Id,
                state));

        serverSession.Update();

        Assert.Equal(
            20,
            serverWorld.Get<TestComponent>(
                serverEntity).Value);

        Assert.True(
            clientChannel.SendDespawn(
                clientConnection.Id,
                networkId));

        serverSession.Update();

        Assert.False(
            serverWorld.Exists(
                serverEntity));

        Assert.False(
            serverMap.TryGetEntity(
                networkId,
                out _));

        Assert.True(
            serverConnection.Id.IsValid);
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