using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public readonly record struct ChunkStreamingInterest
{
    public ChunkStreamingInterest(
        ChunkPosition center)
    {
        Center =
            center;
    }

    public ChunkPosition Center { get; }
}