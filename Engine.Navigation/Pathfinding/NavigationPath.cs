namespace Engine.Navigation.Pathfinding;

public sealed class NavigationPath
{
    public NavigationPath(
        IReadOnlyList<NavigationCoordinate> coordinates,
        float totalCost)
    {
        ArgumentNullException.ThrowIfNull(
            coordinates);

        if (coordinates.Count == 0)
        {
            throw new ArgumentException(
                "Navigation path cannot be empty.",
                nameof(coordinates));
        }

        if (float.IsNaN(totalCost) ||
            float.IsInfinity(totalCost) ||
            totalCost < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalCost));
        }

        Coordinates =
            coordinates.ToArray();

        TotalCost =
            totalCost;
    }

    public IReadOnlyList<NavigationCoordinate> Coordinates { get; }

    public float TotalCost { get; }
}