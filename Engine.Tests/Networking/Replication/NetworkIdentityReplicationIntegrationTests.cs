using Engine.ECS;
using Engine.Networking.Authority;
using Engine.Networking.Replication;
using Engine.Networking.Topology;
using Engine.Serialization.Binary;

namespace Engine.Tests.Networking.Replication;

public sealed class NetworkIdentityReplicationIntegrationTests
{
    [Fact]
    public void Reconcile_WhenIdentityIsRemoved_UnmapsEntityAndCaptureFails()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var networkId = new NetworkEntityId(9012);

        world.Add(
            entity,
            new NetworkIdentity(networkId));

        world.Add(
            entity,
            new ReplicatedTestComponent
            {
                Value = 42
            });

        var entityMap = new NetworkEntityMap(world);

        var authority = new FixedNetworkAuthority(
            new NetworkNodeId(1),
            new NetworkNodeId(1));

        using var synchronizer = new NetworkIdentityMapSynchronizer(
            world,
            entityMap,
            authority);

        var registry = new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new ReplicatedTestComponentSerializer());

        var stateService = new ReplicationStateService(registry);

        Assert.Equal(1, synchronizer.Reconcile());
        Assert.Equal(1, entityMap.Count);

        // Сущность остаётся в мире, но теряет сетевую идентичность.
        world.Remove<NetworkIdentity>(entity);

        Assert.Equal(1, synchronizer.Reconcile());

        Assert.Equal(0, entityMap.Count);

        Assert.False(
            entityMap.TryGetNetworkId(
                entity,
                out _));

        // Нельзя захватывать сетевое состояние сущности без mapping.
        Assert.Throws<InvalidOperationException>(
            () => stateService.Capture(
                world,
                entityMap,
                entity,
                SerializationContext.Default));

        Assert.True(world.Exists(entity));
        Assert.True(world.Has<ReplicatedTestComponent>(entity));
    }

    [Fact]
    public void Spawn_WhenIdentityAlreadyMapsEntity_UpdatesExistingEntity()
    {
        using var sourceWorld = new World();
        using var targetWorld = new World();

        var networkId = new NetworkEntityId(5678);

        // Сущность-источник, с которой захватываем сетевое состояние.
        var sourceEntity = sourceWorld.CreateEntity();

        sourceWorld.Add(
            sourceEntity,
            new ReplicatedTestComponent
            {
                Value = 42
            });

        var sourceMap = new NetworkEntityMap(sourceWorld);
        sourceMap.Register(sourceEntity, networkId);

        // Сущность уже существует на клиенте и имеет NetworkIdentity.
        var targetEntity = targetWorld.CreateEntity();

        targetWorld.Add(
            targetEntity,
            new NetworkIdentity(networkId));

        targetWorld.Add(
            targetEntity,
            new ReplicatedTestComponent
            {
                Value = 1
            });

        var targetMap = new NetworkEntityMap(targetWorld);

        var authority = new FixedNetworkAuthority(
            new NetworkNodeId(1),
            new NetworkNodeId(1));

        using var synchronizer = new NetworkIdentityMapSynchronizer(
            targetWorld,
            targetMap,
            authority);

        Assert.Equal(1, synchronizer.Reconcile());

        var registry = new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new ReplicatedTestComponentSerializer());

        var stateService = new ReplicationStateService(registry);

        var state = stateService.Capture(
            sourceWorld,
            sourceMap,
            sourceEntity,
            SerializationContext.Default);

        var entityService = new ReplicationEntityService(stateService);

        // Повторный Spawn должен использовать существующую сущность.
        var spawnedEntity = entityService.Spawn(
            targetWorld,
            targetMap,
            state,
            SerializationContext.Default);

        Assert.Equal(targetEntity, spawnedEntity);
        Assert.True(targetWorld.Exists(targetEntity));

        Assert.Equal(1, targetMap.Count);
        Assert.Single(targetWorld.Inspector.GetEntities());

        Assert.Equal(
            42,
            targetWorld.Get<ReplicatedTestComponent>(
                targetEntity).Value);

        Assert.Equal(
            networkId,
            targetWorld.Get<NetworkIdentity>(
                targetEntity).Id);
    }

    [Fact]
    public void Reconcile_RegistersIdentityAndCaptureUsesItsNetworkId()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var networkId = new NetworkEntityId(1234);

        world.Add(
            entity,
            new NetworkIdentity(networkId));

        world.Add(
            entity,
            new ReplicatedTestComponent
            {
                Value = 42
            });

        var entityMap = new NetworkEntityMap(world);

        var authority = new FixedNetworkAuthority(
            new NetworkNodeId(1),
            new NetworkNodeId(1));

        using var synchronizer =
            new NetworkIdentityMapSynchronizer(
                world,
                entityMap,
                authority);

        var changes = synchronizer.Reconcile();

        Assert.Equal(1, changes);

        Assert.True(
            entityMap.TryGetNetworkId(
                entity,
                out var mappedId));

        Assert.Equal(networkId, mappedId);

        var registry = new ReplicatedComponentRegistry();

        registry.Register(
            "test.component",
            new ReplicatedTestComponentSerializer());

        var stateService = new ReplicationStateService(registry);

        var state = stateService.Capture(
            world,
            entityMap,
            entity,
            SerializationContext.Default);

        Assert.Equal(networkId, state.Id);

        var component = Assert.Single(state.Components);

        Assert.Equal(
            "test.component",
            component.Id);

        var restored = BinarySerializer.Deserialize(
            component.Payload.Span,
            new ReplicatedTestComponentSerializer());

        Assert.Equal(42, restored.Value);

        // Повторная синхронизация ничего не меняет.
        Assert.Equal(0, synchronizer.Reconcile());

        Assert.Equal(1, entityMap.Count);
    }

    private struct ReplicatedTestComponent
    {
        public int Value { get; set; }
    }

    private sealed class ReplicatedTestComponentSerializer
        : IBinarySerializer<ReplicatedTestComponent>
    {
        public void Serialize(
            ref SerializationWriter writer,
            ReplicatedTestComponent value)
        {
            writer.WriteInt32(value.Value);
        }

        public ReplicatedTestComponent Deserialize(
            ref SerializationReader reader)
        {
            return new ReplicatedTestComponent
            {
                Value = reader.ReadInt32()
            };
        }
    }
}