using Engine.Networking.Replication;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class ReplicationModelTests
{
    [Fact]
    public void NetworkIdentity_StoresNetworkEntityId()
    {
        var id =
            new NetworkEntityId(
                42);

        var identity =
            new NetworkIdentity(
                id);

        Assert.Equal(
            id,
            identity.Id);
    }

    [Fact]
    public void Registry_RegistersComponentByStableIdAndType()
    {
        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.position",
            new TestComponentSerializer());

        Assert.Contains(
            "test.position",
            registry.Ids);
    }

    [Fact]
    public void Registry_RejectsDuplicateId()
    {
        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.value",
            new TestComponentSerializer());

        Assert.Throws<InvalidOperationException>(
            () =>
                registry.Register(
                    "test.value",
                    new OtherComponentSerializer()));
    }

    private readonly record struct TestComponent(
        int Value);

    private readonly record struct OtherComponent(
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

    [Fact]
    public void NetworkEntityMap_MapsEntityToNetworkId()
    {
        using var world =
            new Engine.ECS.World();

        var entity =
            world.CreateEntity();

        var map =
            new NetworkEntityMap(
                world);

        var networkId =
            new NetworkEntityId(
                100);

        map.Register(
            entity,
            networkId);

        Assert.Equal(
            1,
            map.Count);

        Assert.True(
            map.TryGetNetworkId(
                entity,
                out var resolvedNetworkId));

        Assert.Equal(
            networkId,
            resolvedNetworkId);

        Assert.True(
            map.TryGetEntity(
                networkId,
                out var resolvedEntity));

        Assert.Equal(
            entity,
            resolvedEntity);
    }

    [Fact]
    public void NetworkEntityMap_RejectsDuplicateNetworkId()
    {
        using var world =
            new Engine.ECS.World();

        var first =
            world.CreateEntity();

        var second =
            world.CreateEntity();

        var map =
            new NetworkEntityMap(
                world);

        var networkId =
            new NetworkEntityId(
                100);

        map.Register(
            first,
            networkId);

        Assert.Throws<InvalidOperationException>(
            () =>
                map.Register(
                    second,
                    networkId));
    }

    [Fact]
    public void ReplicatedComponentRegistry_CapturesAndAppliesComponent()
    {
        using var world =
            new Engine.ECS.World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new TestComponent(
                42));

        var registry =
            new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new TestComponentSerializer());

        var entry =
            registry.GetByType(
                typeof(TestComponent));

        var payload =
            entry.Capture(
                world,
                entity,
                SerializationContext.Default);

        world.Remove<TestComponent>(
            entity);

        entry.Apply(
            world,
            entity,
            payload,
            SerializationContext.Default);

        Assert.Equal(
            42,
            world.Get<TestComponent>(
                entity).Value);
    }

    private sealed class OtherComponentSerializer :
        IBinarySerializer<OtherComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            OtherComponent value)
        {
            writer.WriteInt32(
                value.Value);
        }

        public OtherComponent Deserialize(
            ref SerializationReader reader)
        {
            return new OtherComponent(
                reader.ReadInt32());
        }
    }
}