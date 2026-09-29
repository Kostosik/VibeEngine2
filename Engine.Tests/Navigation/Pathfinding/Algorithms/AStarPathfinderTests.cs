using Engine.Navigation;
using Engine.Navigation.Maps;
using Engine.Navigation.Pathfinding.Algorithms;

namespace Engine.Tests.Navigation;

public sealed class NavigationPathfinderTests
{
    [Fact]
    public void FindsMinimumCostPathWhenTraversalCostsAreBelowOne()
    {
        var map =
            new NavigationGrid(
                5,
                5);

        var costs =
            new float[,]
            {
            { 0.1f, 5.0f, 0.1f, 0.1f, 0.1f },
            { 1.0f, 0.1f, 5.0f, 1.0f, 1.0f },
            { 1.0f, 0.2f, 5.0f, 1.0f, 5.0f },
            { 5.0f, 1.0f, 5.0f, 0.2f, 0.1f },
            { 1.0f, 1.0f, 5.0f, 0.1f, 0.1f }
            };

        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                map.SetTraversalCost(
                    new NavigationCoordinate(
                        x,
                        y),
                    costs[y, x]);
            }
        }

        var pathfinder =
            new AStarPathfinder();

        var found =
            pathfinder.TryFindPath(
                map,
                new NavigationCoordinate(0, 0),
                new NavigationCoordinate(4, 4),
                out var path);

        Assert.True(found);
        Assert.NotNull(path);

        Assert.Equal(
            7.6f,
            path!.TotalCost,
            4);
    }

    [Fact]
    public void FindsPathAroundBlockedCell()
    {
        var map =
            new NavigationGrid(
                3,
                3);

        map.SetTraversable(
            new NavigationCoordinate(1, 0),
            false);

        var pathfinder =
            new AStarPathfinder();

        var found =
            pathfinder.TryFindPath(
                map,
                new NavigationCoordinate(0, 0),
                new NavigationCoordinate(2, 0),
                out var path);

        Assert.True(
            found);

        Assert.NotNull(
            path);

        Assert.Equal(
            new NavigationCoordinate(0, 0),
            path!.Coordinates[0]);

        Assert.Equal(
            new NavigationCoordinate(2, 0),
            path.Coordinates[^1]);

        Assert.Equal(
            4.0f,
            path.TotalCost);
    }

    [Fact]
    public void ReturnsFalseWhenGoalIsBlocked()
    {
        var map =
            new NavigationGrid(
                3,
                3);

        map.SetTraversable(
            new NavigationCoordinate(2, 2),
            false);

        var pathfinder =
            new AStarPathfinder();

        var found =
            pathfinder.TryFindPath(
                map,
                new NavigationCoordinate(0, 0),
                new NavigationCoordinate(2, 2),
                out var path);

        Assert.False(
            found);

        Assert.Null(
            path);
    }

    [Fact]
    public void UsesTraversalCost()
    {
        var map =
            new NavigationGrid(
                3,
                2);

        map.SetTraversalCost(
            new NavigationCoordinate(1, 0),
            10.0f);

        var pathfinder =
            new AStarPathfinder();

        var found =
            pathfinder.TryFindPath(
                map,
                new NavigationCoordinate(0, 0),
                new NavigationCoordinate(2, 0),
                out var path);

        Assert.True(
            found);

        Assert.NotNull(
            path);

        Assert.Equal(
            4.0f,
            path!.TotalCost);
    }
}