using System.Text;
using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Content;

namespace Engine.Tests.Content;

public sealed class TilemapDefinitionJsonSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_PreserveDefinition()
    {
        var original = new TilemapDefinition(
            TilemapDefinition.CurrentFormatVersion,
            "TileSets/Terrain.tileset.json",
            3,
            2,
            new uint[] { 1, 1, 2, 1, 0, 2 });

        var serialized =
            JsonTilemapDefinitionSerializer.Serialize(original);

        var restored =
            JsonTilemapDefinitionSerializer.Deserialize(
                new AssetPath("Maps/Level.tilemap.json"),
                serialized);

        Assert.Equal(original.FormatVersion, restored.FormatVersion);
        Assert.Equal(original.TileSetPath, restored.TileSetPath);
        Assert.Equal(original.Width, restored.Width);
        Assert.Equal(original.Height, restored.Height);

        Assert.Equal(
            original.Tiles.ToArray(),
            restored.Tiles.ToArray());

        Assert.Equal(2u, restored.GetTile(2, 0));
        Assert.Equal(0u, restored.GetTile(1, 1));
    }

    [Fact]
    public void Deserialize_RejectsIncorrectTileCount()
    {
        const string json = """
        {
          "formatVersion": 1,
          "tileSetPath": "TileSets/Terrain.tileset.json",
          "width": 2,
          "height": 2,
          "tiles": [1, 2, 3]
        }
        """;

        Assert.Throws<InvalidDataException>(
            () => JsonTilemapDefinitionSerializer.Deserialize(
                new AssetPath("Maps/Level.tilemap.json"),
                Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_RejectsTileSetPathOutsideAssets()
    {
        const string json = """
        {
          "formatVersion": 1,
          "tileSetPath": "../Terrain.tileset.json",
          "width": 1,
          "height": 1,
          "tiles": [1]
        }
        """;

        Assert.Throws<InvalidDataException>(
            () => JsonTilemapDefinitionSerializer.Deserialize(
                new AssetPath("Maps/Level.tilemap.json"),
                Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_RejectsNegativeTileValue()
    {
        const string json = """
        {
          "formatVersion": 1,
          "tileSetPath": "TileSets/Terrain.tileset.json",
          "width": 1,
          "height": 1,
          "tiles": [-1]
        }
        """;

        Assert.Throws<InvalidDataException>(
            () => JsonTilemapDefinitionSerializer.Deserialize(
                new AssetPath("Maps/Level.tilemap.json"),
                Encoding.UTF8.GetBytes(json)));
    }
}