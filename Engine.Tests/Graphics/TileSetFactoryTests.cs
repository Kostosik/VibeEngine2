using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;

namespace Engine.Tests.Graphics;

public sealed class TileSetFactoryTests
{
    [Fact]
    public void Create_ResolvesTilesFromDifferentAtlases()
    {
        var definition = new TileSetDefinition(
            1,
            new[]
            {
                new TileSetAtlasDefinition(
                    "terrain",
                    "Textures/terrain.png",
                    32,
                    32),

                new TileSetAtlasDefinition(
                    "decorations",
                    "Textures/decorations.png",
                    16,
                    16)
            },
            new[]
            {
                new TileSetTileDefinition(
                    10,
                    "terrain",
                    1),

                new TileSetTileDefinition(
                    100,
                    "decorations",
                    2)
            });

        using var resources =
            new TestTextureResourceManager();

        var tileSet =
            TileSetFactory.Create(
                definition,
                resources);

        Assert.True(
            tileSet.TryGet(
                10,
                out var terrain));

        Assert.Equal(
            new TextureHandle(10),
            terrain.Texture);

        Assert.Equal(
            new Rectangle(0.5f, 0.0f, 0.5f, 1.0f),
            terrain.UV);

        Assert.True(
            tileSet.TryGet(
                100,
                out var decoration));

        Assert.Equal(
            new TextureHandle(20),
            decoration.Texture);

        Assert.Equal(
            new Rectangle(0.5f, 0.0f, 0.25f, 1.0f),
            decoration.UV);
    }

    [Fact]
    public void Create_RejectsInvalidAtlasIndex()
    {
        var definition = new TileSetDefinition(
            1,
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
                    2)
            });

        using var resources =
            new TestTextureResourceManager();

        Assert.Throws<InvalidDataException>(
            () => TileSetFactory.Create(
                definition,
                resources));
    }

    private sealed class TestTextureResourceManager :
        ITextureResourceManager
    {
        public TextureAtlas LoadAtlas(
            AssetPath path,
            int tileWidth,
            int tileHeight)
        {
            return path.Value switch
            {
                "Textures/terrain.png" =>
                    new TextureAtlas(
                        new TextureHandle(10),
                        new TextureDescription(
                            64,
                            32,
                            TextureFormat.Rgba8),
                        tileWidth,
                        tileHeight),

                "Textures/decorations.png" =>
                    new TextureAtlas(
                        new TextureHandle(20),
                        new TextureDescription(
                            64,
                            16,
                            TextureFormat.Rgba8),
                        tileWidth,
                        tileHeight),

                _ => throw new InvalidOperationException(
                    $"Unexpected texture path '{path}'.")
            };
        }

        public TextureHandle Load(AssetPath path) =>
            throw new NotSupportedException();

        public bool IsLoaded(AssetPath path) => false;

        public bool TryGet(
            AssetPath path,
            out TextureHandle texture)
        {
            texture = TextureHandle.Invalid;
            return false;
        }

        public bool TryGetDescription(
            AssetPath path,
            out TextureDescription description)
        {
            description = default;
            return false;
        }

        public bool Unload(AssetPath path) => false;

        public void UnloadAll()
        {
        }

        public void Dispose()
        {
        }
    }
}