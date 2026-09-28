using Engine.Navigation.Chunks;

namespace Engine.Navigation.Maps;

public sealed class ChunkedNavigationMap : INavigationMap
{
    private readonly Dictionary<NavigationChunkCoordinate, NavigationChunk> _chunks = new();

    public int ChunkWidth { get; }

    public int ChunkHeight { get; }

    public ChunkedNavigationMap(
        int chunkWidth,
        int chunkHeight)
    {
        if (chunkWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkWidth));

        if (chunkHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkHeight));

        ChunkWidth = chunkWidth;
        ChunkHeight = chunkHeight;
    }

    public void AddChunk(NavigationChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        if (chunk.Width != ChunkWidth || chunk.Height != ChunkHeight)
        {
            throw new ArgumentException(
                "Navigation chunk dimensions must match the map chunk dimensions.",
                nameof(chunk));
        }

        if (!_chunks.TryAdd(chunk.Coordinate, chunk))
        {
            throw new InvalidOperationException(
                $"Navigation chunk {chunk.Coordinate} is already registered.");
        }
    }

    public bool RemoveChunk(NavigationChunkCoordinate coordinate)
    {
        return _chunks.Remove(coordinate);
    }

    public bool TryGetChunk(
        NavigationChunkCoordinate coordinate,
        out NavigationChunk? chunk)
    {
        return _chunks.TryGetValue(coordinate, out chunk);
    }

    public bool Contains(NavigationCoordinate coordinate)
    {
        var chunkCoordinate = ToChunkCoordinate(coordinate);

        if (!_chunks.TryGetValue(chunkCoordinate, out var chunk))
            return false;

        var localCoordinate = ToLocalCoordinate(
            coordinate,
            chunkCoordinate);

        return chunk.Grid.Contains(localCoordinate);
    }

    public bool IsTraversable(NavigationCoordinate coordinate)
    {
        var (chunk, localCoordinate) = Resolve(coordinate);

        return chunk.Grid.IsTraversable(localCoordinate);
    }

    public float GetTraversalCost(NavigationCoordinate coordinate)
    {
        var (chunk, localCoordinate) = Resolve(coordinate);

        return chunk.Grid.GetTraversalCost(localCoordinate);
    }

    public void SetTraversable(
        NavigationCoordinate coordinate,
        bool traversable)
    {
        var (chunk, localCoordinate) = Resolve(coordinate);

        chunk.Grid.SetTraversable(
            localCoordinate,
            traversable);
    }

    public void SetTraversalCost(
        NavigationCoordinate coordinate,
        float cost)
    {
        var (chunk, localCoordinate) = Resolve(coordinate);

        chunk.Grid.SetTraversalCost(
            localCoordinate,
            cost);
    }

    public NavigationChunkCoordinate ToChunkCoordinate(
        NavigationCoordinate coordinate)
    {
        return new NavigationChunkCoordinate(
            FloorDivide(coordinate.X, ChunkWidth),
            FloorDivide(coordinate.Y, ChunkHeight));
    }

    public NavigationCoordinate ToLocalCoordinate(
        NavigationCoordinate coordinate,
        NavigationChunkCoordinate chunkCoordinate)
    {
        return new NavigationCoordinate(
            coordinate.X - chunkCoordinate.X * ChunkWidth,
            coordinate.Y - chunkCoordinate.Y * ChunkHeight);
    }

    private (NavigationChunk Chunk, NavigationCoordinate LocalCoordinate) Resolve(
        NavigationCoordinate coordinate)
    {
        var chunkCoordinate = ToChunkCoordinate(coordinate);

        if (!_chunks.TryGetValue(chunkCoordinate, out var chunk))
        {
            throw new InvalidOperationException(
                $"Navigation chunk {chunkCoordinate} is not registered.");
        }

        var localCoordinate = ToLocalCoordinate(
            coordinate,
            chunkCoordinate);

        if (!chunk.Grid.Contains(localCoordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        return (chunk, localCoordinate);
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;

        if (remainder != 0 && value < 0)
            quotient--;

        return quotient;
    }
}