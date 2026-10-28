namespace Engine.Graphics.Tilemaps;

public sealed class TileSetDefinition
{
    public const int CurrentFormatVersion = 1;

    public TileSetDefinition(
        int formatVersion,
        IReadOnlyList<TileSetAtlasDefinition> atlases,
        IReadOnlyList<TileSetTileDefinition> tiles)
    {
        if (formatVersion != CurrentFormatVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                $"Unsupported TileSet format version '{formatVersion}'.");
        }

        ArgumentNullException.ThrowIfNull(atlases);
        ArgumentNullException.ThrowIfNull(tiles);

        var atlasArray = atlases.ToArray();
        var tileArray = tiles.ToArray();

        var atlasIds = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var atlas in atlasArray)
        {
            ArgumentNullException.ThrowIfNull(atlas);

            if (!atlasIds.Add(atlas.Id))
            {
                throw new ArgumentException(
                    $"Duplicate atlas ID '{atlas.Id}'.",
                    nameof(atlases));
            }
        }

        var tileIds = new HashSet<uint>();

        foreach (var tile in tileArray)
        {
            ArgumentNullException.ThrowIfNull(tile);

            if (!tileIds.Add(tile.Id))
            {
                throw new ArgumentException(
                    $"Duplicate tile ID '{tile.Id}'.",
                    nameof(tiles));
            }

            if (!atlasIds.Contains(tile.AtlasId))
            {
                throw new ArgumentException(
                    $"Tile '{tile.Id}' references unknown atlas " +
                    $"'{tile.AtlasId}'.",
                    nameof(tiles));
            }
        }

        FormatVersion = formatVersion;
        Atlases = Array.AsReadOnly(atlasArray);
        Tiles = Array.AsReadOnly(tileArray);
    }

    public int FormatVersion { get; }

    public IReadOnlyList<TileSetAtlasDefinition> Atlases { get; }

    public IReadOnlyList<TileSetTileDefinition> Tiles { get; }
}

public sealed class TileSetAtlasDefinition
{
    public TileSetAtlasDefinition(
        string id,
        string texturePath,
        int tileWidth,
        int tileHeight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);

        if (tileWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileWidth));
        }

        if (tileHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileHeight));
        }

        Id = id.Trim();

        TexturePath = texturePath
            .Replace('\\', '/')
            .Trim('/');

        ArgumentException.ThrowIfNullOrWhiteSpace(TexturePath);

        TileWidth = tileWidth;
        TileHeight = tileHeight;
    }

    public string Id { get; }

    public string TexturePath { get; }

    public int TileWidth { get; }

    public int TileHeight { get; }
}

public sealed class TileSetTileDefinition
{
    public TileSetTileDefinition(
        uint id,
        string atlasId,
        int atlasIndex)
    {
        if (id == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "Tile ID 0 is reserved for empty cells.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(atlasId);

        if (atlasIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(atlasIndex));
        }

        Id = id;
        AtlasId = atlasId.Trim();
        AtlasIndex = atlasIndex;
    }

    public uint Id { get; }

    public string AtlasId { get; }

    public int AtlasIndex { get; }
}