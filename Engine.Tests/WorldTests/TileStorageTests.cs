using ChunkSize = Engine.Worlds.Spatial.ChunkSize;
using LocalPosition = Engine.Worlds.Spatial.LocalPosition;
using Tile = Engine.Worlds.Tiles.Tile;
using TileStorage = Engine.Worlds.Tiles.TileStorage;

namespace Engine.Tests.WorldTests;

public sealed class TileStorageTests
{
    [Fact]
    public void ConstructorCreatesStorageWithCorrectSize()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        Assert.Equal(
            4,
            storage.Width);

        Assert.Equal(
            3,
            storage.Height);

        Assert.Equal(
            12,
            storage.Count);
    }

    [Fact]
    public void NewStorageContainsEmptyTiles()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        var tile =
            storage.Get(
                new LocalPosition(
                    2,
                    1));

        Assert.Equal(
            Tile.Empty,
            tile);
    }

    [Fact]
    public void SetAndGetWorkCorrectly()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        var position =
            new LocalPosition(
                2,
                1);

        var tile =
            new Tile(123);

        storage.Set(
            position,
            tile);

        Assert.Equal(
            tile,
            storage.Get(position));
    }

    [Fact]
    public void DifferentPositionsStoreDifferentTiles()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        var first =
            new LocalPosition(
                0,
                0);

        var second =
            new LocalPosition(
                1,
                0);

        storage.Set(
            first,
            new Tile(10));

        storage.Set(
            second,
            new Tile(20));

        Assert.Equal(
            new Tile(10),
            storage.Get(first));

        Assert.Equal(
            new Tile(20),
            storage.Get(second));
    }

    [Fact]
    public void FillSetsAllTiles()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        var tile =
            new Tile(42);

        storage.Fill(tile);

        foreach (var value in storage.AsReadOnlySpan())
        {
            Assert.Equal(
                tile,
                value);
        }
    }

    [Fact]
    public void InvalidXThrows()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                storage.Get(
                    new LocalPosition(
                        4,
                        0));
            });
    }

    [Fact]
    public void InvalidYThrows()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                storage.Get(
                    new LocalPosition(
                        0,
                        3));
            });
    }

    [Fact]
    public void WritableSpanChangesStorage()
    {
        var storage =
            new TileStorage(
                new ChunkSize(
                    4,
                    3));

        var span =
            storage.AsSpan();

        span[0] =
            new Tile(100);

        Assert.Equal(
            new Tile(100),
            storage.Get(
                new LocalPosition(
                    0,
                    0)));
    }
}