using Engine.ECS.Persistence;
using Engine.Worlds;
using Engine.Worlds.Chunks;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Tests.WorldTests;

public sealed class WorldPersistenceTests
{
    [Fact]
    public void Restore_RejectsInvalidLaterChunkBeforeMutatingWorld()
    {
        var chunkSize =
            new ChunkSize(
                4,
                4);

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                chunkSize,
                ecsWorld);

        var existingEntity =
            ecsWorld.CreateEntity();

        ecsWorld.Add(
            existingEntity,
            new TestValue(
                42));

        using var stateEcsWorld =
            new Engine.ECS.World();

        _ = stateEcsWorld.CreateEntity();

        var replacementEntity =
            stateEcsWorld.CreateEntity();

        stateEcsWorld.Add(
            replacementEntity,
            new TestValue(
                99));

        var state =
            new WorldSaveState(
                chunkSize,
                stateEcsWorld.CaptureState(),
                new[]
                {
                    new ChunkSaveState(
                        new ChunkPosition(
                            0,
                            0),
                        ChunkResidencyState.Loaded,
                        ChunkSimulationState.Suspended,
                        ChunkPresentationState.Irrelevant,
                        new Tile[16]),

                    new ChunkSaveState(
                        new ChunkPosition(
                            1,
                            0),
                        ChunkResidencyState.Loaded,
                        ChunkSimulationState.Suspended,
                        ChunkPresentationState.Irrelevant,
                        new Tile[15])
                });

        Assert.Throws<InvalidDataException>(
            () =>
            {
                WorldPersistence.Restore(
                    world,
                    state);
            });

        Assert.True(
            world.EcsWorld.Exists(
                existingEntity));

        Assert.Equal(
            42,
            world.EcsWorld
                .Get<TestValue>(
                    existingEntity)
                .Value);

        Assert.False(
            world.EcsWorld.Exists(
                replacementEntity));

        Assert.Empty(
            world.GetChunkRecords());

        Assert.Equal(
            0,
            world.SpatialIndex.EntityCount);
    }

    private readonly record struct TestValue(
        int Value);
}