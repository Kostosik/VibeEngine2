using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;

namespace Engine.Worlds.Persistence;

public interface IChunkPersistence
{
    void Save(
        ChunkSaveState state);

    bool TryLoad(
        ChunkPosition position,
        out ChunkSaveState? state);

    void Remove(
        ChunkPosition position);
}