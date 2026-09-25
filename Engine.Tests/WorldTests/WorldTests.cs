using Engine.Worlds.Chunks;
using Chunk = Engine.Worlds.Chunk;
using ChunkPosition = Engine.Worlds.Spatial.ChunkPosition;
using ChunkSize = Engine.Worlds.Spatial.ChunkSize;
using LocalPosition = Engine.Worlds.Spatial.LocalPosition;
using World = Engine.Worlds.World;
using WorldPosition = Engine.Worlds.Spatial.WorldPosition;

namespace Engine.Tests.WorldTests;

public sealed class WorldTests
{
    Engine.ECS.World ecsWorld =
    new Engine.ECS.World();
    [Fact]
    public void ConstructorStoresChunkSize()
    {
        var chunkSize =
            new ChunkSize(
                32,
                32);

        var world =
            new Engine.Worlds.World(chunkSize, ecsWorld);

        Assert.Equal(
            chunkSize,
            world.ChunkSize);
    }

    [Fact]
    public void CreateChunkCreatesChunk()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new ChunkPosition(
                2,
                3);

        var chunk =
            world.CreateChunk(position);

        Assert.NotNull(chunk);

        Assert.Equal(
            position,
            chunk.Position);

        Assert.Equal(
            1,
            world.ChunkCount);
    }

    [Fact]
    public void TryGetChunkReturnsCreatedChunk()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new ChunkPosition(
                2,
                3);

        var created =
            world.CreateChunk(position);

        var found =
            world.TryGetChunk(
                position,
                out var chunk);

        Assert.True(found);
        Assert.Same(created, chunk);
    }

    [Fact]
    public void TryGetChunkReturnsFalseForMissingChunk()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var found =
            world.TryGetChunk(
                new ChunkPosition(
                    10,
                    20),
                out var chunk);

        Assert.False(found);
        Assert.Null(chunk);
    }

    [Fact]
    public void CreateChunkThrowsForDuplicatePosition()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new ChunkPosition(
                1,
                1);

        world.CreateChunk(position);

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                world.CreateChunk(position);
            });
    }

    [Fact]
    public void GetOrCreateChunkCreatesMissingChunk()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new ChunkPosition(
                4,
                5);

        var chunk =
            world.GetOrCreateChunk(position);

        Assert.NotNull(chunk);

        Assert.Equal(
            position,
            chunk.Position);

        Assert.Equal(
            1,
            world.ChunkCount);
    }

    [Fact]
    public void GetOrCreateChunkReturnsExistingChunk()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new ChunkPosition(
                4,
                5);

        var first =
            world.GetOrCreateChunk(position);

        var second =
            world.GetOrCreateChunk(position);

        Assert.Same(
            first,
            second);

        Assert.Equal(
            1,
            world.ChunkCount);
    }

    [Fact]
    public void RemoveChunkRemovesExistingChunk()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(
                    32,
                    32),
                ecsWorld);

        var position =
            new ChunkPosition(
                1,
                2);

        world.CreateChunk(
            position);

        var record =
            world.Chunks.Get(
                position);

        record.Lifecycle.SetSimulation(
            ChunkSimulationState.Suspended);

        Assert.True(
            world.RemoveChunk(
                position));

        Assert.False(
            world.TryGetChunk(
                position,
                out _));
    }

    [Fact]
    public void RemoveChunkReturnsFalseForMissingChunk()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var removed =
            world.RemoveChunk(
                new ChunkPosition(
                    1,
                    2));

        Assert.False(removed);
    }

    [Fact]
    public void GetChunkPositionReturnsCorrectChunk()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new WorldPosition(
                33,
                65);

        var chunkPosition =
            world.GetChunkPosition(position);

        Assert.Equal(
            new ChunkPosition(
                1,
                2),
            chunkPosition);
    }

    [Fact]
    public void GetLocalPositionReturnsCorrectLocalPosition()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new WorldPosition(
                33,
                65);

        var localPosition =
            world.GetLocalPosition(position);

        Assert.Equal(
            new LocalPosition(
                1,
                1),
            localPosition);
    }

    [Fact]
    public void NegativeWorldPositionReturnsCorrectChunkPosition()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new WorldPosition(
                -33,
                -1);

        var chunkPosition =
            world.GetChunkPosition(position);

        Assert.Equal(
            new ChunkPosition(
                -2,
                -1),
            chunkPosition);
    }

    [Fact]
    public void NegativeWorldPositionReturnsCorrectLocalPosition()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var position =
            new WorldPosition(
                -33,
                -1);

        var localPosition =
            world.GetLocalPosition(position);

        Assert.Equal(
            new LocalPosition(
                31,
                31),
            localPosition);
    }

    [Fact]
    public void ToWorldPositionReturnsCorrectWorldPosition()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var worldPosition =
            world.ToWorldPosition(
                new ChunkPosition(
                    2,
                    3),
                new LocalPosition(
                    5,
                    7));

        Assert.Equal(
            new WorldPosition(
                69,
                103),
            worldPosition);
    }

    [Fact]
    public void ToWorldPositionWorksWithNegativeChunkPosition()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var worldPosition =
            world.ToWorldPosition(
                new ChunkPosition(
                    -2,
                    -3),
                new LocalPosition(
                    5,
                    7));

        Assert.Equal(
            new WorldPosition(
                -59,
                -89),
            worldPosition);
    }

    [Fact]
    public void ToWorldPositionThrowsForInvalidLocalX()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                world.ToWorldPosition(
                    new ChunkPosition(
                        0,
                        0),
                    new LocalPosition(
                        32,
                        0));
            });
    }

    [Fact]
    public void ToWorldPositionThrowsForInvalidLocalY()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                world.ToWorldPosition(
                    new ChunkPosition(
                        0,
                        0),
                    new LocalPosition(
                        0,
                        32));
            });
    }

    [Fact]
    public void TryGetNeighborReturnsExistingNeighbor()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var centerPosition =
            new ChunkPosition(
                0,
                0);

        var neighborPosition =
            new ChunkPosition(
                1,
                0);

        world.CreateChunk(centerPosition);

        var expected =
            world.CreateChunk(
                neighborPosition);

        var found =
            world.TryGetNeighbor(
                centerPosition,
                1,
                0,
                out var neighbor);

        Assert.True(found);
        Assert.Same(expected, neighbor);
    }

    [Fact]
    public void TryGetNeighborReturnsFalseForMissingNeighbor()
    {
        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    32,
                    32), ecsWorld);

        var centerPosition =
            new ChunkPosition(
                0,
                0);

        world.CreateChunk(centerPosition);

        var found =
            world.TryGetNeighbor(
                centerPosition,
                1,
                0,
                out var neighbor);

        Assert.False(found);
        Assert.Null(neighbor);
    }
}