using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Networking.Replication;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class ReplicationEntityServiceTests
{
    [Fact]
    public void Spawn_CreatesMappedEntityWithReplicatedComponents()
    {
        using var sourceWorld =
            new World();

        var sourceEntity =
            sourceWorld.CreateEntity();

        sourceWorld.Add(
            sourceEntity,
            new TestComponent
            {
                Value = 42
            });

        var registry =
            CreateRegistry();

        var stateService =
            new ReplicationStateService(
                registry);

        var sourceMap =
            new NetworkEntityMap(
                sourceWorld);

        sourceMap.Register(
            sourceEntity,
            new NetworkEntityId(100));

        var state =
            stateService.Capture(
                sourceWorld,
                sourceMap,
                sourceEntity,
                SerializationContext.Default);

        using var targetWorld =
            new World();

        var targetMap =
            new NetworkEntityMap(
                targetWorld);

        var entityService =
            new ReplicationEntityService(
                stateService);

        var entity =
            entityService.Spawn(
                targetWorld,
                targetMap,
                state,
                SerializationContext.Default);

        Assert.True(
            targetWorld.Exists(entity));

        Assert.True(
            targetMap.TryGetEntity(
                state.Id,
                out var mappedEntity));

        Assert.Equal(
            entity,
            mappedEntity);

        Assert.Equal(
            42,
            targetWorld.Get<TestComponent>(
                entity).Value);
    }

    [Fact]
    public void Update_ChangesExistingReplicatedEntity()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent
            {
                Value = 10
            });

        var map =
            new NetworkEntityMap(
                world);

        var networkId =
            new NetworkEntityId(200);

        map.Register(
            entity,
            networkId);

        var registry =
            CreateRegistry();

        var stateService =
            new ReplicationStateService(
                registry);

        var entityService =
            new ReplicationEntityService(
                stateService);

        var state =
            new ReplicatedEntityState(
                networkId,
                new[]
                {
                    CreateComponentState(
                        registry,
                        new TestComponent
                        {
                            Value = 99
                        })
                });

        entityService.Update(
            world,
            map,
            state,
            SerializationContext.Default);

        Assert.Equal(
            99,
            world.Get<TestComponent>(
                entity).Value);
    }

    [Fact]
    public void Despawn_RemovesEntityAndNetworkMapping()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        var map =
            new NetworkEntityMap(
                world);

        var networkId =
            new NetworkEntityId(300);

        map.Register(
            entity,
            networkId);

        var entityService =
            new ReplicationEntityService(
                new ReplicationStateService(
                    CreateRegistry()));

        var result =
            entityService.Despawn(
                world,
                map,
                networkId);

        Assert.True(
            result);

        Assert.False(
            world.Exists(entity));

        Assert.False(
            map.TryGetEntity(
                networkId,
                out _));
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

    private static ReplicatedComponentState CreateComponentState(
        ReplicatedComponentRegistry registry,
        TestComponent component)
    {
        var payload =
            BinarySerializer.Serialize(
                component,
                new TestComponentSerializer(),
                SerializationContext.Default);

        return new ReplicatedComponentState(
            "test.component",
            payload);
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
                Value = reader.ReadInt32()
            };
        }
    }
}