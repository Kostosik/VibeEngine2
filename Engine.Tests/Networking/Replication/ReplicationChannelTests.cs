using Engine.ECS;
using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Packets;
using Engine.Networking.Replication;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;
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

        using var clientChannel =
            new ReplicationChannel(
                clientSession,
                clientWorld,
                clientMap,
                clientStateService,
                clientEntityService,
                clientAuthority,
                serverNode,
                SerializationContext.Default);

        using var serverChannel =
            new ReplicationChannel(
                serverSession,
                serverWorld,
                serverMap,
                serverStateService,
                serverEntityService,
                serverAuthority,
                serverNode,
                SerializationContext.Default);

        var serverEntity =
    serverWorld.CreateEntity();

        var networkId =
            new NetworkEntityId(100);

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

        clientSession.Update();

        Assert.True(
            clientMap.TryGetEntity(
                networkId,
                out var clientEntity));

        Assert.True(
            clientWorld.Exists(
                clientEntity));

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

        clientSession.Update();

        Assert.Equal(
            20,
            clientWorld.Get<TestComponent>(
                clientEntity).Value);

        Assert.True(
            serverChannel.SendDespawn(
                serverConnection.Id,
                networkId));

        clientSession.Update();

        Assert.False(
            clientWorld.Exists(
                clientEntity));

        Assert.False(
            clientMap.TryGetEntity(
                networkId,
                out _));
    }

    [Fact]
    public void NonAuthorityPeerReplication_IsIgnored()
    {
        using var authorityTransport =
            new LoopbackTransport();

        using var clientTransport =
            new LoopbackTransport();

        using var otherTransport =
            new LoopbackTransport();

        using var authoritySession =
            new NetworkSession(
                authorityTransport);

        using var clientSession =
            new NetworkSession(
                clientTransport);

        using var otherSession =
            new NetworkSession(
                otherTransport);

        var authorityEndpoint =
            new NetworkEndpoint(
                "authority",
                1900);

        var clientEndpoint =
            new NetworkEndpoint(
                "client",
                1901);

        var otherEndpoint =
            new NetworkEndpoint(
                "other",
                1902);

        authoritySession.Start(
            authorityEndpoint);

        clientSession.Start(
            clientEndpoint);

        otherSession.Start(
            otherEndpoint);

        var clientToAuthority =
            clientSession.Connect(
                authorityEndpoint);

        var otherToClient =
            otherSession.Connect(
                clientEndpoint);

        authoritySession.Update();
        clientSession.Update();

        var authorityNode =
            new NetworkNode(
                new NetworkNodeId(1),
                authorityEndpoint);

        var clientNode =
            new NetworkNode(
                new NetworkNodeId(2),
                clientEndpoint);

        var otherNode =
            new NetworkNode(
                new NetworkNodeId(3),
                otherEndpoint);

        var authority =
            new FixedNetworkAuthority(
                authorityNode.Id,
                authorityNode.Id);

        var clientAuthority =
            new FixedNetworkAuthority(
                clientNode.Id,
                authorityNode.Id);

        var otherAuthority =
            new FixedNetworkAuthority(
                otherNode.Id,
                authorityNode.Id);

        using var authorityWorld =
            new World();

        using var clientWorld =
            new World();

        using var otherWorld =
            new World();

        var authorityMap =
            new NetworkEntityMap(
                authorityWorld);

        var clientMap =
            new NetworkEntityMap(
                clientWorld);

        var otherMap =
            new NetworkEntityMap(
                otherWorld);

        var authorityRegistry =
            CreateRegistry();

        var clientRegistry =
            CreateRegistry();

        var otherRegistry =
            CreateRegistry();

        var authorityStateService =
            new ReplicationStateService(
                authorityRegistry);

        var clientStateService =
            new ReplicationStateService(
                clientRegistry);

        var otherStateService =
            new ReplicationStateService(
                otherRegistry);

        var authorityEntityService =
            new ReplicationEntityService(
                authorityStateService);

        var clientEntityService =
            new ReplicationEntityService(
                clientStateService);

        var otherEntityService =
            new ReplicationEntityService(
                otherStateService);

        using var authorityChannel =
            new ReplicationChannel(
                authoritySession,
                authorityWorld,
                authorityMap,authorityStateService,
                authorityEntityService,
                authority,
                authorityNode,
                SerializationContext.Default);

        using var clientChannel =
            new ReplicationChannel(
                clientSession,
                clientWorld,
                clientMap, clientStateService,
                clientEntityService,
                clientAuthority,
                authorityNode,
                SerializationContext.Default);

        using var otherChannel =
            new ReplicationChannel(
                otherSession,
                otherWorld,
                otherMap,otherStateService,
                otherEntityService,
                otherAuthority,
                authorityNode,
                SerializationContext.Default);

        var clientEntity =
            clientWorld.CreateEntity();

        var networkId =
            new NetworkEntityId(700);

        clientMap.Register(
            clientEntity,
            networkId);

        clientWorld.Add(
            clientEntity,
            new TestComponent
            {
                Value = 10
            });

        var maliciousState =
            new ReplicatedEntityState(
                networkId,
                new[]
                {
                new ReplicatedComponentState(
                    "test.component",
                    BinarySerializer.Serialize(
                        new TestComponent
                        {
                            Value = 999
                        },
                        new TestComponentSerializer(),
                        SerializationContext.Default))
                });

        var payload =
            BinarySerializer.Serialize(
                ReplicationMessage.Update(
                    maliciousState),
                new ReplicationMessageSerializer(),
                SerializationContext.Default);

        var packet =
            new NetworkPacket(
                new PacketId(3),
                NetworkChannel.Reliable,
                payload);

        Assert.True(
            otherSession.Send(
                otherToClient.Id,
                packet));

        clientSession.Update();

        Assert.Equal(
            10,
            clientWorld.Get<TestComponent>(
                clientEntity).Value);

        Assert.True(
            clientWorld.Exists(
                clientEntity));

        Assert.True(
            clientToAuthority.Id.IsValid);
    }

    [Fact]
    public void NonAuthorityCannotSendUpdate()
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
                "authority-client",
                1800);

        var serverEndpoint =
            new NetworkEndpoint(
                "authority-server",
                1801);

        clientSession.Start(
            clientEndpoint);

        serverSession.Start(
            serverEndpoint);

        var clientConnection =
            clientSession.Connect(
                serverEndpoint);

        serverSession.Update();

        var clientNode =
            new NetworkNode(
                new NetworkNodeId(2),
                clientEndpoint);

        var serverNode =
            new NetworkNode(
                new NetworkNodeId(1),
                serverEndpoint);

        var clientAuthority =
            new FixedNetworkAuthority(
                clientNode.Id,
                serverNode.Id);

        var serverAuthority =
            new FixedNetworkAuthority(
                serverNode.Id,
                serverNode.Id);

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
                clientMap, clientStateService,
                clientEntityService,
                clientAuthority,
                serverNode,
                SerializationContext.Default);

        using var serverChannel =
            new ReplicationChannel(
                serverSession,
                serverWorld,
                serverMap,serverStateService,
                serverEntityService,
                serverAuthority,
                serverNode,
                SerializationContext.Default);

        var entity =
            clientWorld.CreateEntity();

        var networkId =
            new NetworkEntityId(500);

        clientMap.Register(
            entity,
            networkId);

        clientWorld.Add(
            entity,
            new TestComponent
            {
                Value = 10
            });

        var state =
            clientStateService.Capture(
                clientWorld,
                clientMap,
                entity,
                SerializationContext.Default);

        Assert.False(
            clientChannel.SendUpdate(
                clientConnection.Id,
                state));
    }

    [Fact]
    public void NewConnection_ReceivesExistingReplicatedEntities()
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
                "initial-sync-client",
                2000);

        var serverEndpoint =
            new NetworkEndpoint(
                "initial-sync-server",
                2001);

        clientSession.Start(
            clientEndpoint);

        serverSession.Start(
            serverEndpoint);

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

        using var serverChannel =
            new ReplicationChannel(
                serverSession,
                serverWorld,
                serverMap,
                serverStateService,
                serverEntityService,
                serverAuthority,
                serverNode,
                SerializationContext.Default);

        using var clientChannel =
            new ReplicationChannel(
                clientSession,
                clientWorld,
                clientMap,
                clientStateService,
                clientEntityService,
                clientAuthority,
                serverNode,
                SerializationContext.Default);

        CreateServerEntity(
            serverWorld,
            serverMap,
            serverStateService,
            new NetworkEntityId(100),
            10);

        CreateServerEntity(
            serverWorld,
            serverMap,
            serverStateService,
            new NetworkEntityId(200),
            20);

        clientSession.Connect(
            serverEndpoint);

        serverSession.Update();

        clientSession.Update();

        Assert.Equal(
            2,
            clientMap.Count);

        Assert.True(
            clientMap.TryGetEntity(
                new NetworkEntityId(100),
                out var firstClientEntity));

        Assert.True(
            clientMap.TryGetEntity(
                new NetworkEntityId(200),
                out var secondClientEntity));

        Assert.Equal(
            10,
            clientWorld.Get<TestComponent>(
                firstClientEntity).Value);

        Assert.Equal(
            20,
            clientWorld.Get<TestComponent>(
                secondClientEntity).Value);
    }

    [Fact]
    public void InitialSyncFollowedByUpdateAndDespawn_PreservesReplicationOrder()
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
                "join-client",
                2100);

        var serverEndpoint =
            new NetworkEndpoint(
                "join-server",
                2101);

        clientSession.Start(
            clientEndpoint);

        serverSession.Start(
            serverEndpoint);

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

        using var serverChannel =
            new ReplicationChannel(
                serverSession,
                serverWorld,
                serverMap,
                serverStateService,
                serverEntityService,
                serverAuthority,
                serverNode,
                SerializationContext.Default);

        using var clientChannel =
            new ReplicationChannel(
                clientSession,
                clientWorld,
                clientMap,
                clientStateService,
                clientEntityService,
                clientAuthority,
                serverNode,
                SerializationContext.Default);

        var serverEntity =
            serverWorld.CreateEntity();

        var networkId =
            new NetworkEntityId(600);

        serverMap.Register(
            serverEntity,
            networkId);

        serverWorld.Add(
            serverEntity,
            new TestComponent
            {
                Value = 10
            });

        clientSession.Connect(
            serverEndpoint);

        serverSession.Update();

        serverWorld.Get<TestComponent>(
            serverEntity).Value = 20;

        var state =
            serverStateService.Capture(
                serverWorld,
                serverMap,
                serverEntity,
                SerializationContext.Default);

        Assert.True(
            serverChannel.SendUpdate(
                serverSession.Connections.Single().Id,
                state));

        clientSession.Update();

        Assert.Single(
            clientMap.GetMappings());

        Assert.True(
            clientMap.TryGetEntity(
                networkId,
                out var clientEntity));

        Assert.Equal(
            20,
            clientWorld.Get<TestComponent>(
                clientEntity).Value);

        Assert.True(
            serverChannel.SendDespawn(
                serverSession.Connections.Single().Id,
                networkId));

        clientSession.Update();

        Assert.False(
            clientWorld.Exists(
                clientEntity));

        Assert.False(
            clientMap.TryGetEntity(
                networkId,
                out _));
    }

    private static void CreateServerEntity(
    World world,
    NetworkEntityMap map,
    ReplicationStateService stateService,
    NetworkEntityId networkId,
    int value)
    {
        var entity =
            world.CreateEntity();

        map.Register(
            entity,
            networkId);

        world.Add(
            entity,
            new TestComponent
            {
                Value = value
            });
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