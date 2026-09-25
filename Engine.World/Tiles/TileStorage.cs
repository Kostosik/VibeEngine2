using Engine.Memory.Pools;
using Engine.Worlds.Spatial;
using System.Runtime.InteropServices;

namespace Engine.Worlds.Tiles;

public sealed class TileStorage :
    IDisposable
{
    private readonly PooledBuffer<Tile> _tiles;

    private bool _disposed;

    public TileStorage(
        ChunkSize size)
    {
        var count =
            (long)size.Width *
            size.Height;

        if (count > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Chunk is too large.");
        }

        Width =
            size.Width;

        Height =
            size.Height;

        var pool =
            new MemoryPool<Tile>();

        _tiles =
            pool.Rent(
                (int)count);
    }

    public int Width { get; }

    public int Height { get; }

    public int Count =>
        _tiles.Length;

    public Tile Get(
        LocalPosition position)
    {
        EnsureNotDisposed();

        return _tiles[
            GetIndex(position)];
    }

    public void Set(
        LocalPosition position,
        Tile tile)
    {
        EnsureNotDisposed();

        _tiles[
            GetIndex(position)] =
            tile;
    }

    public void Fill(
        Tile tile)
    {
        EnsureNotDisposed();

        _tiles
            .Span
            .Fill(tile);
    }

    public Span<Tile> AsSpan()
    {
        EnsureNotDisposed();

        return _tiles.Span;
    }

    public ReadOnlySpan<Tile> AsReadOnlySpan()
    {
        EnsureNotDisposed();

        return _tiles.Span;
    }

    public ReadOnlySpan<uint> AsValueReadOnlySpan()
    {
        EnsureNotDisposed();

        return MemoryMarshal.Cast<Tile, uint>(
            _tiles.Span);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _tiles.Dispose();

        _disposed =
            true;
    }

    private int GetIndex(
        LocalPosition position)
    {
        ValidatePosition(position);

        return
            position.Y * Width +
            position.X;
    }

    private void ValidatePosition(
        LocalPosition position)
    {
        if (position.X < 0 ||
            position.X >= Width)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Local X must be in range [0, {Width}).");
        }

        if (position.Y < 0 ||
            position.Y >= Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Local Y must be in range [0, {Height}).");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}