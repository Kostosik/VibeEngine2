using Engine.Jobs.Jobs;
using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

internal sealed class ChunkLoadJob :
    IJob
{
    private readonly IChunkLoader _loader;

    private readonly ChunkPosition _position;

    private readonly ChunkSize _size;

    private readonly ChunkLoadResult _result;

    public ChunkLoadJob(
        IChunkLoader loader,
        ChunkPosition position,
        ChunkSize size,
        ChunkLoadResult result)
    {
        ArgumentNullException.ThrowIfNull(
            loader);

        ArgumentNullException.ThrowIfNull(
            result);

        _loader =
            loader;

        _position =
            position;

        _size =
            size;

        _result =
            result;
    }

    public void Execute()
    {
        _result.Data =
            _loader.Load(
                _position,
                _size);
    }
}

internal sealed class ChunkLoadResult
{
    public ChunkData? Data { get; set; }
}