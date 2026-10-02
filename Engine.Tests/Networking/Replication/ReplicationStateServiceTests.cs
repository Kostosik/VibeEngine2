using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Networking.Replication;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class ReplicationStateServiceTests
{
    [Fact]
    public void Apply_RemovesMissingReplicatedComponent_AndPreservesUnreplicatedComponent()
    {
        using var sourceWorld =
            new World();

        using var targetWorld =
            new World();

        var sourceEntity =
            sourceWorld.CreateEntity();

        var targetEntity =
            targetWorld.CreateEntity();

        sourceWorld.Add(
            sourceEntity,
            new TestComponent(
                10));

        sourceWorld.Add(
            sourceEntity,
            new SecondTestComponent(
                20));

        targetWorld.Add(
            targetEntity,
            new TestComponent(
                10));

        targetWorld.Add(
            targetEntity,
            new SecondTestComponent(
                20));

        targetWorld.Add(
            targetEntity,
            new UnreplicatedComponent(
                99));

        var sourceMap =
            new NetworkEntityMap(
                sourceWorld);

        var targetMap =
            new NetworkEntityMap(
                targetWorld);

        var networkId =
            new NetworkEntityId(
                300);

        sourceMap.Register(
            sourceEntity,
            networkId);

        targetMap.Register(
            targetEntity,
            networkId);

        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new TestComponentSerializer());

        registry.Register(
            "second.component",
            new SecondTestComponentSerializer());

        var service =
            new ReplicationStateService(
                registry);

        var initialState =
            service.Capture(
                sourceWorld,
                sourceMap,
                sourceEntity,
                SerializationContext.Default);

        service.Apply(
            targetWorld,
            targetMap,
            initialState,
            SerializationContext.Default);

        sourceWorld.Remove<SecondTestComponent>(
            sourceEntity);

        var updatedState =
            service.Capture(
                sourceWorld,
                sourceMap,
                sourceEntity,
                SerializationContext.Default);

        service.Apply(
            targetWorld,
            targetMap,
            updatedState,
            SerializationContext.Default);

        Assert.True(
            targetWorld.Has<TestComponent>(
                targetEntity));

        Assert.Equal(
            10,
            targetWorld.Get<TestComponent>(
                targetEntity).Value);

        Assert.False(
            targetWorld.Has<SecondTestComponent>(
                targetEntity));

        Assert.True(
            targetWorld.Has<UnreplicatedComponent>(
                targetEntity));

        Assert.Equal(
            99,
            targetWorld.Get<UnreplicatedComponent>(
                targetEntity).Value);
    }

    [Fact]
    public void Capture_ProducesStateForRegisteredComponentsOnly()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent(
                42));

        world.Add(
            entity,
            new UnreplicatedComponent(
                99));

        var map =
            new NetworkEntityMap(
                world);

        var networkId =
            new NetworkEntityId(
                100);

        map.Register(
            entity,
            networkId);

        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new TestComponentSerializer());

        var service =
            new ReplicationStateService(
                registry);

        var state =
            service.Capture(
                world,
                map,
                entity,
                SerializationContext.Default);

        Assert.Equal(
            networkId,
            state.Id);

        Assert.Single(
            state.Components);

        var component =
            state.Components[0];

        Assert.Equal(
            "test.component",
            component.Id);

        var restored =
            BinarySerializer.Deserialize(
                component.Payload.Span,
                new TestComponentSerializer());

        Assert.Equal(
            42,
            restored.Value);
    }

    [Fact]
    public void Apply_InvalidComponentPayload_DoesNotMutateWorld()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent(
                10));

        world.Add(
            entity,
            new SecondTestComponent(
                20));

        var map =
            new NetworkEntityMap(
                world);

        var networkId =
            new NetworkEntityId(
                400);

        map.Register(
            entity,
            networkId);

        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new TestComponentSerializer());

        registry.Register(
            "second.component",
            new SecondTestComponentSerializer());

        var service =
            new ReplicationStateService(
                registry);

        var validPayload =
            BinarySerializer.Serialize(
                new TestComponent(
                    50),
                new TestComponentSerializer(),
                SerializationContext.Default);

        var state =
            new ReplicatedEntityState(
                networkId,
                new[]
                {
                new ReplicatedComponentState(
                    "test.component",
                    validPayload),

                new ReplicatedComponentState(
                    "second.component",
                    new byte[]
                    {
                        1
                    })
                });

        Assert.Throws<InvalidDataException>(
            () =>
                service.Apply(
                    world,
                    map,
                    state,
                    SerializationContext.Default));

        Assert.Equal(
            10,
            world.Get<TestComponent>(
                entity).Value);

        Assert.Equal(
            20,
            world.Get<SecondTestComponent>(
                entity).Value);
    }

    [Fact]
    public void Apply_RestoresReplicatedComponent()
    {
        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent(
                10));

        var map =
            new NetworkEntityMap(
                world);

        var networkId =
            new NetworkEntityId(
                200);

        map.Register(
            entity,
            networkId);

        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new TestComponentSerializer());

        var service =
            new ReplicationStateService(
                registry);

        var state =
            service.Capture(
                world,
                map,
                entity,
                SerializationContext.Default);

        world.Get<TestComponent>(
            entity).Value = 999;

        service.Apply(
            world,
            map,
            state,
            SerializationContext.Default);

        Assert.Equal(
            10,
            world.Get<TestComponent>(
                entity).Value);
    }

    private struct TestComponent { public TestComponent(int value) { Value = value; } public int Value { get; set; } }

    private readonly record struct UnreplicatedComponent(
        int Value);

    private sealed class SecondTestComponentSerializer :
    IBinarySerializer<SecondTestComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            SecondTestComponent value)
        {
            writer.WriteInt32(
                value.Value);
        }

        public SecondTestComponent Deserialize(
            ref SerializationReader reader)
        {
            return new SecondTestComponent(
                reader.ReadInt32());
        }
    }

    private struct SecondTestComponent
    {
        public SecondTestComponent(
            int value)
        {
            Value =
                value;
        }

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
            return new TestComponent(
                reader.ReadInt32());
        }
    }
}