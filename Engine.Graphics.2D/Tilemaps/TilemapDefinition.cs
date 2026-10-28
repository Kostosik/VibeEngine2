namespace Engine.Graphics.Tilemaps;

public sealed class TilemapDefinition
{
    public const int CurrentFormatVersion = 1;

    public TilemapDefinition(
        int formatVersion,
        string tileSetPath,
        int width,
        int height,
        IReadOnlyList<uint> tiles)
    {
        if (formatVersion != CurrentFormatVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                $"Unsupported Tilemap format version '{formatVersion}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tileSetPath);
        ArgumentNullException.ThrowIfNull(tiles);

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var normalizedPath = tileSetPath.Trim().Replace('\\', '/');

        var isDriveRooted =
            normalizedPath.Length >= 2 &&
            char.IsAsciiLetter(normalizedPath[0]) &&
            normalizedPath[1] == ':';

        if (Path.IsPathRooted(normalizedPath) ||
            normalizedPath.StartsWith('/') ||
            isDriveRooted)
        {
            throw new ArgumentException(
                "TileSet path must be relative to the Assets directory.",
                nameof(tileSetPath));
        }

        var segments = normalizedPath.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 ||
            segments.Any(segment => segment is "." or "..") ||
            !normalizedPath.EndsWith(
                ".tileset.json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "TileSet path must reference a valid .tileset.json asset.",
                nameof(tileSetPath));
        }

        var normalizedTileSetPath = string.Join("/", segments);
        var tileArray = tiles.ToArray();
        var expectedCount = checked(width * height);

        if (tileArray.Length != expectedCount)
        {
            throw new ArgumentException(
                $"Expected {expectedCount} tile values, " +
                $"but received {tileArray.Length}.",
                nameof(tiles));
        }

        FormatVersion = formatVersion;
        TileSetPath = normalizedTileSetPath;
        Width = width;
        Height = height;
        Tiles = Array.AsReadOnly(tileArray);
    }

    public int FormatVersion { get; }

    public string TileSetPath { get; }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<uint> Tiles { get; }

    public uint GetTile(int x, int y)
    {
        if ((uint)x >= (uint)Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if ((uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return Tiles[y * Width + x];
    }
}