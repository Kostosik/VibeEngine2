using Engine.Navigation;
using Engine.Navigation.Maps;
using Xunit;

namespace Engine.Tests.Navigation.Maps;

public sealed class NavigationGridTests
{
    [Fact]
    public void SetTraversable_ChangesCellState()
    {
        var grid = new NavigationGrid(4, 4);
        var coordinate = new NavigationCoordinate(1, 2);

        Assert.True(grid.IsTraversable(coordinate));

        grid.SetTraversable(coordinate, false);

        Assert.False(grid.IsTraversable(coordinate));

        grid.SetTraversable(coordinate, true);

        Assert.True(grid.IsTraversable(coordinate));
    }

    [Fact]
    public void SetTraversalCost_ChangesCellCost()
    {
        var grid = new NavigationGrid(4, 4);

        var coordinate = new NavigationCoordinate(1, 2);

        Assert.Equal(1f, grid.GetTraversalCost(coordinate));

        grid.SetTraversalCost(coordinate, 7.5f);

        Assert.Equal(7.5f, grid.GetTraversalCost(coordinate));
    }
}