using Engine.Navigation;
using Engine.Navigation.Chunks;
using Engine.Navigation.Maps;
using Xunit;

namespace Engine.Tests.Navigation.Maps;

public sealed class ChunkedNavigationMapTests
{
    [Fact]
    public void Contains_UsesRegisteredChunks()
    {
        var map = new ChunkedNavigationMap(4, 4);

        map.AddChunk(
            new NavigationChunk(
                new NavigationChunkCoordinate(1, 0),
                4,
                4));

        Assert.False(
            map.Contains(new NavigationCoordinate(3, 1)));

        Assert.True(
            map.Contains(new NavigationCoordinate(4, 1)));

        Assert.True(
            map.Contains(new NavigationCoordinate(7, 3)));

        Assert.False(
            map.Contains(new NavigationCoordinate(8, 3)));
    }

    [Fact]
    public void Coordinates_AreConvertedAcrossChunkBoundaries()
    {
        var map = new ChunkedNavigationMap(4, 4);

        var chunk = new NavigationChunk(
            new NavigationChunkCoordinate(1, -1),
            4,
            4);

        map.AddChunk(chunk);

        var worldCoordinate = new NavigationCoordinate(4, -1);

        Assert.Equal(
            new NavigationChunkCoordinate(1, -1),
            map.ToChunkCoordinate(worldCoordinate));

        Assert.Equal(
            new NavigationCoordinate(0, 3),
            map.ToLocalCoordinate(
                worldCoordinate,
                new NavigationChunkCoordinate(1, -1)));

        Assert.True(map.Contains(worldCoordinate));
    }

    [Fact]
    public void SetTraversable_UpdatesCorrectChunkCell()
    {
        var map = new ChunkedNavigationMap(4, 4);

        map.AddChunk(
            new NavigationChunk(
                new NavigationChunkCoordinate(1, 0),
                4,
                4));

        var coordinate = new NavigationCoordinate(5, 2);

        Assert.True(map.IsTraversable(coordinate));

        map.SetTraversable(coordinate, false);

        Assert.False(map.IsTraversable(coordinate));
    }
}