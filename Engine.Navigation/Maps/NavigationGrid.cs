using Engine.Navigation;

namespace Engine.Navigation.Maps;

public sealed class NavigationGrid : INavigationMap
{
    private readonly bool[] _traversable;
    private readonly float[] _traversalCosts;

    public int Width { get; }

    public int Height { get; }

    public NavigationGrid(
        int width,
        int height,
        float defaultTraversalCost = 1f)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        if (!float.IsFinite(defaultTraversalCost) || defaultTraversalCost <= 0f)
            throw new ArgumentOutOfRangeException(nameof(defaultTraversalCost));

        Width = width;
        Height = height;

        var cellCount = checked(width * height);

        _traversable = new bool[cellCount];
        _traversalCosts = new float[cellCount];

        Array.Fill(_traversable, true);
        Array.Fill(_traversalCosts, defaultTraversalCost);
    }

    public bool Contains(NavigationCoordinate coordinate)
    {
        return coordinate.X >= 0
            && coordinate.X < Width
            && coordinate.Y >= 0
            && coordinate.Y < Height;
    }

    public bool IsTraversable(NavigationCoordinate coordinate)
    {
        return _traversable[GetIndex(coordinate)];
    }

    public float GetTraversalCost(NavigationCoordinate coordinate)
    {
        return _traversalCosts[GetIndex(coordinate)];
    }

    public void SetTraversable(NavigationCoordinate coordinate, bool traversable)
    {
        _traversable[GetIndex(coordinate)] = traversable;
    }

    public void SetTraversalCost(
        NavigationCoordinate coordinate,
        float cost)
    {
        if (!float.IsFinite(cost) || cost <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cost));

        _traversalCosts[GetIndex(coordinate)] = cost;
    }

    private int GetIndex(NavigationCoordinate coordinate)
    {
        if (!Contains(coordinate))
            throw new ArgumentOutOfRangeException(nameof(coordinate));

        return coordinate.Y * Width + coordinate.X;
    }
}