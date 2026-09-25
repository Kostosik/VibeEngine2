using Engine.Worlds.Tiles;

namespace Engine.Worlds.Chunks;

public sealed class ChunkData :
    IDisposable
{
    private bool _disposed;

    public ChunkData(
        TileStorage tiles)
    {
        ArgumentNullException.ThrowIfNull(
            tiles);

        Tiles =
            tiles;
    }

    public TileStorage Tiles { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Tiles.Dispose();

        _disposed =
            true;
    }
}