using Engine.Jobs.Scheduling;
using Engine.Worlds;
using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Tests.Worlds.Chunks;

public sealed class ChunkStreamingServiceTests
{
    [Fact]
    public void MovingOutsidePreloadRadius_UnloadsAndRestoresChunkFromPersistence()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(
                    4,
                    4),
                ecsWorld);

        using var scheduler =
            new JobScheduler(
                workerCount: 2);

        var service =
            new ChunkStreamingService(
                world,
                new ChunkStreamingPolicy(
                    preloadRadius: 0,
                    presentationRadius: 0),
                new TestChunkLoader(),
                scheduler);

        var first =
            new ChunkPosition(
                0,
                0);

        var second =
            new ChunkPosition(
                10,
                0);

        service.Update(
            new ChunkStreamingInterest(
                first));

        service.FlushLoads();

        service.Update(
            new ChunkStreamingInterest(
                first));

        var chunk =
            world.Chunks.Get(
                first)
            .Chunk;

        Assert.NotNull(
            chunk);

        chunk.Tiles.Set(
            new LocalPosition(
                1,
                1),
            new Tile(
                777));

        service.Update(
            new ChunkStreamingInterest(
                second));

        Assert.Equal(
            ChunkResidencyState.Unloaded,
            world.Chunks
                .Get(first)
                .Lifecycle
                .Residency);

        Assert.True(
            world.ChunkPersistence.TryLoad(
                first,
                out var persisted));

        Assert.NotNull(
            persisted);

        Assert.Equal(
            777u,
            persisted!
                .Tiles[
                    1 + 1 * 4]
                .Value);

        service.FlushLoads();

        service.Update(
            new ChunkStreamingInterest(
                first));

        service.FlushLoads();

        service.Update(
            new ChunkStreamingInterest(
                first));

        var restored =
            world.Chunks
                .Get(first)
                .Chunk;

        Assert.NotNull(
            restored);

        Assert.Equal(
            777u,
            restored!
                .Tiles
                .Get(
                    new LocalPosition(
                        1,
                        1))
                .Value);
    }

    [Fact]
    public void Update_LoadsRequiredChunksWithoutChangingSimulationPolicy()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(
                    32,
                    32),
                ecsWorld);

        using var scheduler =
            new JobScheduler(
                workerCount: 2);

        var service =
            new ChunkStreamingService(
                world,
                new ChunkStreamingPolicy(
                    preloadRadius: 1,
                    presentationRadius: 0),
                new TestChunkLoader(),
                scheduler);

        var center =
            new ChunkPosition(
                10,
                20);

        var request =
            service.Update(
                new ChunkStreamingInterest(
                    center));

        Assert.Equal(
            9,
            request.ResidentChunks.Count);

        Assert.Equal(
            9,
            service.PendingLoadCount);

        service.FlushLoads();

        service.Update(
            new ChunkStreamingInterest(
                center));

        Assert.Equal(
            9,
            world.ChunkCount);

        Assert.Equal(
            9,
            world.Chunks.LoadedCount);

        var centerRecord =
            world.Chunks.Get(
                center);

        Assert.Equal(
            ChunkResidencyState.Loaded,
            centerRecord.Lifecycle.Residency);

        Assert.Equal(
            ChunkSimulationState.Simulating,
            centerRecord.Lifecycle.Simulation);

        Assert.Equal(
            ChunkPresentationState.Relevant,
            centerRecord.Lifecycle.Presentation);

        var remotePosition =
            new ChunkPosition(
                11,
                20);

        var remoteRecord =
            world.Chunks.Get(
                remotePosition);

        Assert.Equal(
            ChunkPresentationState.Irrelevant,
            remoteRecord.Lifecycle.Presentation);

        Assert.Equal(
            ChunkSimulationState.Simulating,
            remoteRecord.Lifecycle.Simulation);
    }
}