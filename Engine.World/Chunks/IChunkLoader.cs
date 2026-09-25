using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public interface IChunkLoader
{
    ChunkData Load(
        ChunkPosition position,
        ChunkSize size);
}