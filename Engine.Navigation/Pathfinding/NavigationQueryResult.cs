namespace Engine.Navigation.Pathfinding;

public enum NavigationQueryStatus
{
    Success,
    StartOutsideMap,
    GoalOutsideMap,
    StartBlocked,
    GoalBlocked,
    NoPath
}

public readonly record struct NavigationQueryResult(
    NavigationQueryStatus Status,
    NavigationPath? Path)
{
    public bool Succeeded => Status == NavigationQueryStatus.Success;

    public static NavigationQueryResult Success(NavigationPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new NavigationQueryResult(
            NavigationQueryStatus.Success,
            path);
    }

    public static NavigationQueryResult Failure(
        NavigationQueryStatus status)
    {
        if (status == NavigationQueryStatus.Success)
            throw new ArgumentException(
                "Failure result cannot use the Success status.",
                nameof(status));

        return new NavigationQueryResult(
            status,
            null);
    }
}