using Engine.Navigation.Maps;

namespace Engine.Navigation.Chunks;

public sealed class NavigationChunk
{
    public NavigationChunkCoordinate Coordinate { get; }

    public NavigationGrid Grid { get; }

    public int Width => Grid.Width;

    public int Height => Grid.Height;

    public NavigationChunk(
        NavigationChunkCoordinate coordinate,
        int width,
        int height,
        float defaultTraversalCost = 1f)
    {
        Coordinate = coordinate;
        Grid = new NavigationGrid(
            width,
            height,
            defaultTraversalCost);
    }
}