using Engine.ECS.Persistence;
using Engine.Worlds.Spatial;

namespace Engine.Worlds.Persistence;

public sealed class WorldSaveState
{
    public WorldSaveState(
        ChunkSize chunkSize,
        EcsWorldState ecs,
        IReadOnlyList<ChunkSaveState> chunks)
    {
        ArgumentNullException.ThrowIfNull(
            ecs);

        ArgumentNullException.ThrowIfNull(
            chunks);

        ChunkSize = chunkSize;
        Ecs = ecs;
        Chunks = chunks.ToArray();
    }

    public ChunkSize ChunkSize { get; }

    public EcsWorldState Ecs { get; }

    public IReadOnlyList<ChunkSaveState> Chunks { get; }
}