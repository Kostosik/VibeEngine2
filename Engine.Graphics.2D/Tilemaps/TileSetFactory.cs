using Engine.Core.Assets;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Tilemaps;

public static class TileSetFactory
{
    public static TileSet Create(
        TileSetDefinition definition,
        ITextureResourceManager resources)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(resources);

        var atlases =
            new Dictionary<string, TextureAtlas>(
                StringComparer.Ordinal);

        foreach (var atlasDefinition in definition.Atlases)
        {
            var atlas =
                resources.LoadAtlas(
                    new AssetPath(
                        atlasDefinition.TexturePath),
                    atlasDefinition.TileWidth,
                    atlasDefinition.TileHeight);

            atlases.Add(
                atlasDefinition.Id,
                atlas);
        }

        var tileSet = new TileSet();

        foreach (var tileDefinition in definition.Tiles)
        {
            var atlas =
                atlases[tileDefinition.AtlasId];

            if ((uint)tileDefinition.AtlasIndex >=
                (uint)atlas.Count)
            {
                throw new InvalidDataException(
                    $"Tile '{tileDefinition.Id}' references atlas index " +
                    $"'{tileDefinition.AtlasIndex}', but atlas " +
                    $"'{tileDefinition.AtlasId}' contains " +
                    $"'{atlas.Count}' regions.");
            }

            tileSet.Set(
                tileDefinition.Id,
                atlas,
                tileDefinition.AtlasIndex);
        }

        return tileSet;
    }
}