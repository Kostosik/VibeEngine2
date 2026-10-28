using Engine.Core.Assets;
using Engine.Editor.Documents;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Content;

namespace Engine.Tests.Editor;

public sealed class TileSetDocumentTests
{
    [Fact]
    public void Open_LoadsDefinitionAndStartsClean()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var path = Path.Combine(
                directory.FullName,
                "World.tileset.json");

            File.WriteAllBytes(
                path,
                JsonTileSetDefinitionSerializer.Serialize(
                    CreateDefinition()));

            var document = TileSetDocument.Open(path);

            Assert.Equal(
                TileSetDocument.TypeId,
                document.DocumentTypeId);

            Assert.Equal(
                Path.GetFullPath(path),
                document.FilePath);

            Assert.Equal(
                "World.tileset.json",
                document.DisplayName);

            Assert.Single(document.Definition.Atlases);
            Assert.Single(document.Definition.Tiles);

            Assert.Equal(
                "Grass",
                document.Definition.Tiles[0].Name);

            Assert.False(document.IsDirty);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void UpdateDefinition_MarksDirtyAndSavePersistsChanges()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var path = Path.Combine(
                directory.FullName,
                "World.tileset.json");

            File.WriteAllBytes(
                path,
                JsonTileSetDefinitionSerializer.Serialize(
                    CreateDefinition()));

            var document = TileSetDocument.Open(path);
            var changedCount = 0;

            document.Changed += () => changedCount++;

            var original = document.Definition;

            document.UpdateDefinition(
                new TileSetDefinition(
                    original.FormatVersion,
                    original.Atlases,
                    new[]
                    {
                        new TileSetTileDefinition(
                            1,
                            "terrain",
                            0,
                            "Grass"),

                        new TileSetTileDefinition(
                            2,
                            "terrain",
                            1,
                            "Dirt")
                    }));

            Assert.True(document.IsDirty);
            Assert.Equal(1, changedCount);

            document.Save();

            Assert.False(document.IsDirty);
            Assert.Equal(2, changedCount);

            var reloaded =
                JsonTileSetDefinitionSerializer.Deserialize(
                    new AssetPath("World.tileset.json"),
                    File.ReadAllBytes(path));

            Assert.Equal(2, reloaded.Tiles.Count);
            Assert.Equal("Grass", reloaded.Tiles[0].Name);
            Assert.Equal("Dirt", reloaded.Tiles[1].Name);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Open_RejectsUnsupportedFileExtension()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var path = Path.Combine(
                directory.FullName,
                "World.json");

            File.WriteAllText(path, "{}");

            Assert.Throws<ArgumentException>(
                () => TileSetDocument.Open(path));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static TileSetDefinition CreateDefinition() =>
        new(
            TileSetDefinition.CurrentFormatVersion,
            new[]
            {
                new TileSetAtlasDefinition(
                    "terrain",
                    "Textures/terrain.png",
                    32,
                    32)
            },
            new[]
            {
                new TileSetTileDefinition(
                    1,
                    "terrain",
                    0,
                    "Grass")
            });
}