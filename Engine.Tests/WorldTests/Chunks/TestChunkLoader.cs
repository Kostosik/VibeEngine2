using Engine.Worlds;
using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Tests.Worlds.Chunks;

internal sealed class TestChunkLoader : IChunkLoader
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