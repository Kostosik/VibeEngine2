using Engine.Jobs.Scheduling;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Worlds.Chunks;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;
using Engine.Tests.Worlds.Chunks;

namespace Engine.Tests.Serialization.SaveLoad;

public sealed class WorldSaveLoadServiceTests
{
    [Fact]
    public void World_RoundTripsThroughRealFile()
    {
        var registry =
            new EcsComponentSerializerRegistry();

        registry.Register(
            "test.value",
            new TestValueSerializer());

        var saveLoad =
            new WorldSaveLoadService(
                registry);

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    4,
                    4),
                ecsWorld);

        var entity =
            world.EcsWorld.CreateEntity();

        world.EcsWorld.Add(
            entity,
            new TestValue(
                42));

        var chunkPosition =
            new ChunkPosition(
                -2,
                3);

        var chunk =
            world.CreateChunk(
                chunkPosition);

        chunk.Tiles.Set(
            new LocalPosition(
                1,
                2),
            new Tile(
                123));

        world.Chunks
            .Get(chunkPosition)
            .Lifecycle
            .SetSimulation(
                ChunkSimulationState.Suspended);

        world.Chunks
            .Get(chunkPosition)
            .Lifecycle
            .SetPresentation(
                ChunkPresentationState.Irrelevant);

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            saveLoad.Save(
                path,
                world);

            using var restoredEcsWorld =
                new Engine.ECS.World();

            var restoredWorld =
                new Engine.Worlds.World(
                    new ChunkSize(
                        4,
                        4),
                    restoredEcsWorld);

            saveLoad.Load(
                path,
                restoredWorld);

            Assert.True(
                restoredWorld.EcsWorld.Exists(
                    entity));

            Assert.Equal(
                42,
                restoredWorld
                    .EcsWorld
                    .Get<TestValue>(
                        entity)
                    .Value);

            Assert.Equal(
                1,
                restoredWorld.ChunkCount);

            Assert.True(
                restoredWorld.TryGetChunk(
                    chunkPosition,
                    out var restoredChunk));

            Assert.NotNull(
                restoredChunk);

            Assert.Equal(
                new Tile(
                    123),
                restoredChunk.Tiles.Get(
                    new LocalPosition(
                        1,
                        2)));

            var lifecycle =
                restoredWorld.Chunks
                    .Get(chunkPosition)
                    .Lifecycle;

            Assert.Equal(
                ChunkSimulationState.Suspended,
                lifecycle.Simulation);

            Assert.Equal(
                ChunkPresentationState.Irrelevant,
                lifecycle.Presentation);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void World_WithUnloadedChunk_PreservesResidencyAfterSaveLoad()
    {
        var registry =
            new EcsComponentSerializerRegistry();

        var saveLoad =
            new WorldSaveLoadService(
                registry);

        using var ecsWorld =
            new Engine.ECS.World();

        var persistence =
            new MemoryChunkPersistence();

        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    4,
                    4),
                ecsWorld,
                persistence);

        var position =
            new ChunkPosition(
                5,
                7);

        var chunk =
            world.CreateChunk(
                position);

        chunk.Tiles.Set(
            new LocalPosition(
                2,
                1),
            new Tile(
                999));

        Assert.True(
            world.UnloadChunk(
                position));

        Assert.Equal(
            ChunkResidencyState.Unloaded,
            world.Chunks
                .Get(position)
                .Lifecycle
                .Residency);

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            saveLoad.Save(
                path,
                world);

            using var restoredEcsWorld =
                new Engine.ECS.World();

            var restoredPersistence =
                new MemoryChunkPersistence();

            var restoredWorld =
                new Engine.Worlds.World(
                    new ChunkSize(
                        4,
                        4),
                    restoredEcsWorld,
                    restoredPersistence);

            saveLoad.Load(
                path,
                restoredWorld);

            var record =
                restoredWorld.Chunks.Get(
                    position);

            Assert.Equal(
                ChunkResidencyState.Unloaded,
                record.Lifecycle.Residency);

            Assert.Null(
                record.Chunk);

            Assert.True(
                restoredPersistence.TryLoad(
                    position,
                    out var persisted));

            Assert.NotNull(
                persisted);

            Assert.Equal(
                999u,
                persisted!
                    .Tiles[
                        2 + 1 * 4]
                    .Value);

            var service =
                new ChunkStreamingService(
                    restoredWorld,
                    new ChunkStreamingPolicy(
                        preloadRadius: 0,
                        presentationRadius: 0),
                    new TestChunkLoader(),
                    new JobScheduler(
                        workerCount: 2));

            // ...
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private readonly record struct TestValue(
        int Value);

    private sealed class TestValueSerializer :
        IBinarySerializer<TestValue>
    {
        public void Serialize(
            ref SerializationWriter writer,
            TestValue value)
        {
            writer.WriteInt32(
                value.Value);
        }

        public TestValue Deserialize(
            ref SerializationReader reader)
        {
            return new TestValue(
                reader.ReadInt32());
        }
    }
}