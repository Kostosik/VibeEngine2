using Engine.Navigation;
using Engine.Navigation.Maps;
using Engine.Navigation.Pathfinding;
using Engine.Navigation.Pathfinding.Algorithms;
using Xunit;

namespace Engine.Tests.Navigation.Pathfinding;

public sealed class NavigationQueryServiceTests
{
    [Fact]
    public void FindPath_ReturnsSuccessfulResult()
    {
        var map = new NavigationGrid(5, 5);
        var service = new NavigationQueryService(
            map,
            new AStarPathfinder());

        var result = service.FindPath(
            new NavigationPathQuery(
                new NavigationCoordinate(0, 0),
                new NavigationCoordinate(4, 0)));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Path);
    }

    [Fact]
    public void FindPath_ReturnsBlockedStatusForBlockedStart()
    {
        var map = new NavigationGrid(5, 5);
        var start = new NavigationCoordinate(0, 0);

        map.SetTraversable(start, false);

        var service = new NavigationQueryService(
            map,
            new AStarPathfinder());

        var result = service.FindPath(
            new NavigationPathQuery(
                start,
                new NavigationCoordinate(4, 0)));

        Assert.False(result.Succeeded);
        Assert.Equal(
            NavigationQueryStatus.StartBlocked,
            result.Status);
        Assert.Null(result.Path);
    }
}