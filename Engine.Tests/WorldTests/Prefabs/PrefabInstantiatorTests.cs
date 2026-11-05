using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Worlds.Prefabs;
using Engine.Worlds.Spatial;

namespace Engine.Tests.WorldTests.Prefabs;

public sealed class PrefabInstantiatorTests
{
    [Fact]
    public void Instantiate_RemapReferencesAndRegistersSpatialEntities()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new Engine.Worlds.World(
            new ChunkSize(16, 16),
            ecsWorld);

        var rootId = new PrefabEntityId(1);
        var childId = new PrefabEntityId(2);

        var prefab = new PrefabAsset(
            "TestPrefab",
            rootId,
            new[]
            {
                new PrefabEntity(
                    rootId,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<WorldPositionComponent>(
                            new WorldPositionComponent(
                                new WorldPosition(10, 20))),

                        new PrefabComponent<TestEntityLink>(
                            new TestEntityLink(
                                new EntityId(2, 0)))
                    }),

                new PrefabEntity(
                    childId,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<TransformParent2D>(
                            new TransformParent2D(
                                new EntityId(1, 0))),

                        new PrefabComponent<WorldPositionComponent>(
                            new WorldPositionComponent(
                                new WorldPosition(12, 22)))
                    })
            });

        var instance = PrefabInstantiator.Instantiate(
            world,
            prefab,
            new WorldPosition(100, 200));

        var root = instance.Root;
        var child = instance.GetEntity(childId);

        Assert.True(world.EcsWorld.Exists(root));
        Assert.True(world.EcsWorld.Exists(child));

        Assert.Equal(
            child,
            world.EcsWorld.Get<TestEntityLink>(root).Target);

        Assert.Equal(
            root,
            world.EcsWorld.Get<TransformParent2D>(child).Parent);

        Assert.Equal(
            new WorldPosition(100, 200),
            world.EcsWorld.Get<WorldPositionComponent>(root).Position);

        Assert.Equal(
            new WorldPosition(102, 202),
            world.EcsWorld.Get<WorldPositionComponent>(child).Position);

        Assert.True(world.SpatialEntities.Contains(root));
        Assert.True(world.SpatialEntities.Contains(child));

        Assert.True(
            world.SpatialIndex.Contains(
                root,
                world.GetChunkPosition(new WorldPosition(100, 200))));

        Assert.True(
            world.SpatialIndex.Contains(
                child,
                world.GetChunkPosition(new WorldPosition(102, 202))));
    }

    [Fact]
    public void Instantiate_WithoutPosition_PreservesPrefabPositions()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new Engine.Worlds.World(
            new ChunkSize(16, 16),
            ecsWorld);

        var rootId = new PrefabEntityId(1);

        var prefab = new PrefabAsset(
            "PositionedPrefab",
            rootId,
            new[]
            {
                new PrefabEntity(
                    rootId,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<WorldPositionComponent>(
                            new WorldPositionComponent(
                                new WorldPosition(-20, 35)))
                    })
            });

        var instance = PrefabInstantiator.Instantiate(
            world,
            prefab);

        Assert.Equal(
            new WorldPosition(-20, 35),
            world.EcsWorld
                .Get<WorldPositionComponent>(instance.Root)
                .Position);

        Assert.True(
            world.SpatialEntities.Contains(instance.Root));
    }

    [Fact]
    public void Instantiate_WhenPositionOffsetOverflows_RollsBackCreatedEntities()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new Engine.Worlds.World(
            new ChunkSize(16, 16),
            ecsWorld);

        var rootId = new PrefabEntityId(1);
        var childId = new PrefabEntityId(2);

        var prefab = new PrefabAsset(
            "OverflowPrefab",
            rootId,
            new[]
            {
                new PrefabEntity(
                    rootId,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<WorldPositionComponent>(
                            new WorldPositionComponent(
                                new WorldPosition(int.MaxValue, 0)))
                    }),

                new PrefabEntity(
                    childId,
                    new PrefabComponent[]
                    {
                        new PrefabComponent<TransformParent2D>(
                            new TransformParent2D(
                                new EntityId(1, 0))),

                        new PrefabComponent<WorldPositionComponent>(
                            new WorldPositionComponent(
                                new WorldPosition(int.MinValue, 0)))
                    })
            });

        Assert.Throws<OverflowException>(
            () => PrefabInstantiator.Instantiate(
                world,
                prefab,
                new WorldPosition(int.MinValue, 0)));

        Assert.Equal(0, world.EcsWorld.EntityCount);
        Assert.Equal(0, world.SpatialIndex.EntityCount);
    }

    private readonly record struct TestEntityLink(EntityId Target)
        : IPrefabEntityReferenceRemappable
    {
        public object RemapPrefabEntityReferences(
            Func<EntityId, EntityId> remap)
        {
            ArgumentNullException.ThrowIfNull(remap);

            return new TestEntityLink(remap(Target));
        }
    }
}