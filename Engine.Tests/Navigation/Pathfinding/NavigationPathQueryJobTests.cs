using Engine.Navigation;
using Engine.Navigation.Maps;
using Engine.Navigation.Pathfinding;
using Engine.Navigation.Pathfinding.Algorithms;

namespace Engine.Tests.Navigation.Pathfinding;

public sealed class NavigationPathQueryJobTests
{
    [Fact]
    public void Result_IsNotSuccessfulBeforeExecute()
    {
        var service =
            new NavigationQueryService(
                new NavigationGrid(4, 4),
                new AStarPathfinder());

        var job =
            new NavigationPathQueryJob(
                service,
                new NavigationPathQuery(
                    new NavigationCoordinate(0, 0),
                    new NavigationCoordinate(3, 0)));

        Assert.False(job.Result.Succeeded);

        Assert.Equal(
            NavigationQueryStatus.NotExecuted,
            job.Result.Status);

        Assert.Null(job.Result.Path);
    }

    [Fact]
    public void Execute_ProducesQueryResult()
    {
        var service =
            new NavigationQueryService(
                new NavigationGrid(4, 4),
                new AStarPathfinder());

        var job =
            new NavigationPathQueryJob(
                service,
                new NavigationPathQuery(
                    new NavigationCoordinate(0, 0),
                    new NavigationCoordinate(3, 0)));

        job.Execute();

        Assert.True(job.Result.Succeeded);
        Assert.NotNull(job.Result.Path);
    }
}