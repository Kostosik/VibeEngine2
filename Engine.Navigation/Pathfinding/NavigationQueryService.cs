using Engine.Jobs.Scheduling;
using Engine.Jobs.Jobs;
using Engine.Navigation.Maps;

namespace Engine.Navigation.Pathfinding;

public sealed class NavigationQueryService
{
    private readonly INavigationMap _map;
    private readonly INavigationPathfinder _pathfinder;

    public NavigationQueryService(
        INavigationMap map,
        INavigationPathfinder pathfinder)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(pathfinder);

        _map = map;
        _pathfinder = pathfinder;
    }

    public JobHandle Schedule(
    JobScheduler scheduler,
    NavigationPathQuery query,
    out NavigationPathQueryJob job,
    params JobHandle[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(
            scheduler);

        ArgumentNullException.ThrowIfNull(
            dependencies);

        job =
            new NavigationPathQueryJob(
                this,
                query);

        return scheduler.Schedule(
            job,
            dependencies);
    }

    public NavigationQueryResult FindPath(
        NavigationPathQuery query)
    {
        if (!_map.Contains(query.Start))
        {
            return NavigationQueryResult.Failure(
                NavigationQueryStatus.StartOutsideMap);
        }

        if (!_map.Contains(query.Goal))
        {
            return NavigationQueryResult.Failure(
                NavigationQueryStatus.GoalOutsideMap);
        }

        if (!_map.IsTraversable(query.Start))
        {
            return NavigationQueryResult.Failure(
                NavigationQueryStatus.StartBlocked);
        }

        if (!_map.IsTraversable(query.Goal))
        {
            return NavigationQueryResult.Failure(
                NavigationQueryStatus.GoalBlocked);
        }

        if (!_pathfinder.TryFindPath(
                _map,
                query.Start,
                query.Goal,
                out var path))
        {
            return NavigationQueryResult.Failure(
                NavigationQueryStatus.NoPath);
        }

        return NavigationQueryResult.Success(
            path!);
    }
}