using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Content;

namespace Engine.Tests.Content;

public sealed class TilemapContentLoaderTests
{
    [Fact]
    public void Load_ReadsTilemapThroughContentManager()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineTilemap-{Guid.NewGuid():N}");

        var directory = Path.Combine(root, "Maps");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(
                Path.Combine(directory, "Level.tilemap.json"),
                """
                {
                  "formatVersion": 1,
                  "tileSetPath": "TileSets/Terrain.tileset.json",
                  "width": 3,
                  "height": 2,
                  "tiles": [1, 1, 2, 1, 0, 2]
                }
                """);

            using var content = new ContentManager(
                new FileAssetSource(root),
                new FileContentCatalog(root),
                new ContentLoaderRegistry());

            content.Register(new JsonTilemapContentLoader());

            var map = content.Load<TilemapDefinition>(
                new AssetPath("Maps/Level.tilemap.json"));

            Assert.Equal(3, map.Width);
            Assert.Equal(2, map.Height);

            Assert.Equal(
                "TileSets/Terrain.tileset.json",
                map.TileSetPath);

            Assert.Equal(2u, map.GetTile(2, 0));
            Assert.Equal(0u, map.GetTile(1, 1));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CanLoad_OnlyMatchesTilemapSuffix()
    {
        var loader = new JsonTilemapContentLoader();

        var tilemap = new Engine.Content.Assets.ContentAsset(
            new AssetPath("Maps/Level.tilemap.json"),
            ".json",
            0L,
            DateTime.UtcNow);

        var tileSet = new Engine.Content.Assets.ContentAsset(
            new AssetPath("TileSets/Terrain.tileset.json"),
            ".json",
            0L,
            DateTime.UtcNow);

        Assert.True(loader.CanLoad(tilemap));
        Assert.False(loader.CanLoad(tileSet));
    }
}