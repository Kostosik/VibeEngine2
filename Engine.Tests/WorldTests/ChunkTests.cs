using ChunkPosition = Engine.Worlds.Spatial.ChunkPosition;
using ChunkSize = Engine.Worlds.Spatial.ChunkSize;
using World = Engine.Worlds.World;

namespace Engine.Tests.WorldTests;

public sealed class ChunkTests
{
    [Fact]
    public void ChunkHasTileStorage()
    {
        var ecsWorld =
    new Engine.ECS.World();

        var world =
            new Engine.Worlds.World(
                new ChunkSize(
                    16,
                    8), ecsWorld);

        var chunk =
            world.CreateChunk(
                new ChunkPosition(
                    2,
                    3));

        Assert.NotNull(
            chunk.Tiles);

        Assert.Equal(
            16,
            chunk.Tiles.Width);

        Assert.Equal(
            8,
            chunk.Tiles.Height);

        Assert.Equal(
            128,
            chunk.Tiles.Count);
    }
}