using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public sealed class ChunkStreamingRequest
{
    internal ChunkStreamingRequest(
        IReadOnlyList<ChunkPosition> residentChunks,
        IReadOnlyList<ChunkPosition> presentationChunks)
    {
        ResidentChunks =
            residentChunks;

        PresentationChunks =
            presentationChunks;
    }

    public IReadOnlyList<ChunkPosition> ResidentChunks { get; }

    public IReadOnlyList<ChunkPosition> PresentationChunks { get; }
}