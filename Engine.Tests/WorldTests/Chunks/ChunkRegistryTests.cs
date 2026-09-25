using Engine.ECS;
using Engine.Worlds;
using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;

namespace Engine.Tests.WorldTests.Chunks;

public sealed class ChunkRegistryTests
{
    [Fact]
    public void RemovedChunk_ReleasesTileStorage()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32),
                ecsWorld);

        var position =
            new ChunkPosition(
                10,
                20);

        var chunk =
            world.CreateChunk(
                position);

        chunk.Tiles.Set(
            new LocalPosition(
                0,
                0),
            default);

        var record =
            world.Chunks.Get(
                position);

        record.Lifecycle.SetSimulation(
            ChunkSimulationState.Suspended);

        Assert.True(
            world.RemoveChunk(
                position));

        Assert.Throws<ObjectDisposedException>(
            () =>
                chunk.Tiles.AsSpan());
    }

    [Fact]
    public void RegisteredChunk_CanBeUnloadedAndRemoved()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32),
                ecsWorld);

        var position =
            new ChunkPosition(
                10,
                20);

        world.CreateChunk(
            position);

        var record =
            world.Chunks.Get(
                position);

        Assert.Equal(
            ChunkResidencyState.Loaded,
            record.Lifecycle.Residency);

        Assert.NotNull(
            record.Chunk);

        record.Lifecycle.SetSimulation(
            ChunkSimulationState.Suspended);

        Assert.True(
            world.RemoveChunk(
                position));

        Assert.False(
            world.Chunks.TryGet(
                position,
                out _));
    }
}