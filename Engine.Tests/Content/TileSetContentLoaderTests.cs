using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Content;

namespace Engine.Tests.Content;

public sealed class TileSetContentLoaderTests
{
    private const string ValidJson = """
    {
      "formatVersion": 1,
      "atlases": [
        {
          "id": "terrain",
          "texturePath": "Textures/terrain.png",
          "tileWidth": 32,
          "tileHeight": 32
        },
        {
          "id": "decorations",
          "texturePath": "Textures/decorations.png",
          "tileWidth": 16,
          "tileHeight": 16
        }
      ],
    "tiles": [
      { "id": 1, "name": "Grass", "atlasId": "terrain", "atlasIndex": 0 },
      { "id": 2, "name": "Dirt", "atlasId": "terrain", "atlasIndex": 1 },
      { "id": 100, "name": "Decoration", "atlasId": "decorations", "atlasIndex": 3 }
    ]
    }
    """;

    [Fact]
    public void Load_ReadsTileSetDefinitionThroughContentManager()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineTileSet-{Guid.NewGuid():N}");

        var directory = Path.Combine(root, "TileSets");
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(
                Path.Combine(directory, "World.tileset.json"),
                ValidJson);

            using var content = new ContentManager(
                new FileAssetSource(root),
                new FileContentCatalog(root),
                new ContentLoaderRegistry());

            content.Register(
                new JsonTileSetContentLoader());

            var definition = content.Load<TileSetDefinition>(
                new AssetPath("TileSets/World.tileset.json"));

            Assert.Equal(
                TileSetDefinition.CurrentFormatVersion,
                definition.FormatVersion);

            Assert.Equal(2, definition.Atlases.Count);
            Assert.Equal(3, definition.Tiles.Count);

            Assert.Equal("terrain", definition.Atlases[0].Id);
            Assert.Equal("Textures/terrain.png", definition.Atlases[0].TexturePath);

            Assert.Equal(1u, definition.Tiles[0].Id);
            Assert.Equal("terrain", definition.Tiles[0].AtlasId);
            Assert.Equal(1, definition.Tiles[1].AtlasIndex);

            Assert.Equal("decorations", definition.Tiles[2].AtlasId);
            Assert.Equal("Grass", definition.Tiles[0].Name);
            Assert.Equal("Dirt", definition.Tiles[1].Name);
            Assert.Equal("Decoration", definition.Tiles[2].Name);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SerializeAndDeserialize_PreserveDefinition()
    {
        var path = new AssetPath("TileSets/World.tileset.json");

        var original =
            JsonTileSetDefinitionSerializer.Deserialize(
                path,
                System.Text.Encoding.UTF8.GetBytes(ValidJson));

        var serialized =
            JsonTileSetDefinitionSerializer.Serialize(original);

        var restored =
            JsonTileSetDefinitionSerializer.Deserialize(
                path,
                serialized);

        Assert.Equal(original.FormatVersion, restored.FormatVersion);
        Assert.Equal(original.Atlases.Count, restored.Atlases.Count);
        Assert.Equal(original.Tiles.Count, restored.Tiles.Count);

        for (var i = 0; i < original.Tiles.Count; i++)
        {
            Assert.Equal(original.Tiles[i].Id, restored.Tiles[i].Id);
            Assert.Equal(original.Tiles[i].AtlasId, restored.Tiles[i].AtlasId);
            Assert.Equal(original.Tiles[i].AtlasIndex, restored.Tiles[i].AtlasIndex);
            Assert.Equal(
    original.Tiles[i].Name,
    restored.Tiles[i].Name);
        }
    }

    [Fact]
    public void Deserialize_RejectsTexturePathOutsideAssets()
    {
        const string json = """
    {
      "formatVersion": 1,
      "atlases": [
        {
          "id": "terrain",
          "texturePath": "../terrain.png",
          "tileWidth": 32,
          "tileHeight": 32
        }
      ],
      "tiles": []
    }
    """;

        Assert.Throws<InvalidDataException>(
            () => JsonTileSetDefinitionSerializer.Deserialize(
                new AssetPath("TileSets/World.tileset.json"),
                System.Text.Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_RejectsUnknownAtlas()
    {
        const string json = """
        {
          "formatVersion": 1,
          "atlases": [],
          "tiles": [
            { "id": 1, "atlasId": "missing", "atlasIndex": 0 }
          ]
        }
        """;

        Assert.Throws<InvalidDataException>(
            () => JsonTileSetDefinitionSerializer.Deserialize(
                new AssetPath("Broken.tileset.json"),
                System.Text.Encoding.UTF8.GetBytes(json)));
    }
}