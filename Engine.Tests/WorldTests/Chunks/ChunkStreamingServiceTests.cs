using Engine.Jobs.Scheduling;
using Engine.Worlds;
using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Tests.Worlds.Chunks;

public sealed class ChunkStreamingServiceTests
{
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

    private sealed class TestChunkLoader :
        IChunkLoader
    {
        public ChunkData Load(
            ChunkPosition position,
            ChunkSize size)
        {
            return new ChunkData(
                new TileStorage(
                    size));
        }
    }
}