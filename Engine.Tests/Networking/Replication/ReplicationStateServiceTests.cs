using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Networking.Replication;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class ReplicationStateServiceTests
{
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