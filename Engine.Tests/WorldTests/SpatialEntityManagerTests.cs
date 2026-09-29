using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.WorldTests;

public sealed class SpatialEntityManagerTests
{
    [Fact]
    public void SetPosition_RepairsMissingSpatialIndexEntry()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(
                    32,
                    32),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            new WorldPositionComponent(
                new WorldPosition(
                    0,
                    0)));

        world.SpatialEntities.SetPosition(
            entity,
            new WorldPosition(
                10,
                0));

        Assert.True(
            world.SpatialIndex.Contains(
                entity,
                new ChunkPosition(
                    0,
                    0)));

        Assert.Equal(
            new WorldPosition(
                10,
                0),
            world.SpatialEntities.GetPosition(
                entity));
    }

    [Fact]
    public void GetChunks_ReturnsSnapshot()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var entity =
            ecsWorld.CreateEntity();

        var index =
            new SpatialIndex();

        var chunk =
            new ChunkPosition(
                1,
                2);

        index.Add(
            entity,
            chunk);

        var chunks =
            index.GetChunks(
                entity);

        var snapshot =
            Assert.IsType<ChunkPosition[]>(
                chunks);

        snapshot[0] =
            new ChunkPosition(
                99,
                99);

        Assert.True(
            index.Contains(
                entity,
                chunk));

        Assert.False(
            index.Contains(
                entity,
                new ChunkPosition(
                    99,
                    99)));
    }
}