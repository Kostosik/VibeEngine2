using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Worlds;

public sealed class Chunk :
    IDisposable
{
    private readonly ChunkData _data;

    private bool _disposed;

    internal Chunk(
        ChunkPosition position,
        ChunkSize size)
        : this(
            position,
            new ChunkData(
                new TileStorage(
                    size)))
    {
    }

    internal Chunk(
        ChunkPosition position,
        ChunkData data)
    {
        ArgumentNullException.ThrowIfNull(
            data);

        Position =
            position;

        _data =
            data;
    }

    public ChunkPosition Position { get; }

    public TileStorage Tiles =>
        _data.Tiles;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _data.Dispose();

        _disposed =
            true;
    }
}