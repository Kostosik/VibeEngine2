using Engine.Core.Math;

namespace Engine.Graphics.Resources;

public sealed class TextureAtlas
{
    private readonly AtlasRegion[]? _regions;
    private readonly int _count;
    private readonly bool _isGrid;

    /// <summary>
    /// Creates a regular grid atlas.
    /// </summary>
    public TextureAtlas(
        TextureHandle texture,
        TextureDescription textureDescription,
        int tileWidth,
        int tileHeight)
    {
        ValidateTexture(
            texture,
            textureDescription);

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

        if (textureDescription.Width % tileWidth != 0)
        {
            throw new ArgumentException(
                "Texture width must be divisible by tile width.",
                nameof(tileWidth));
        }

        if (textureDescription.Height % tileHeight != 0)
        {
            throw new ArgumentException(
                "Texture height must be divisible by tile height.",
                nameof(tileHeight));
        }

        Texture = texture;

        TileWidth = tileWidth;
        TileHeight = tileHeight;

        Columns =
            textureDescription.Width / tileWidth;

        Rows =
            textureDescription.Height / tileHeight;

        _count = checked(Columns * Rows);
        _isGrid = true;
    }

    /// <summary>
    /// Creates an atlas from explicitly defined regions.
    /// Regions must use normalized UV coordinates.
    /// </summary>
    public TextureAtlas(
        TextureHandle texture,
        TextureDescription textureDescription,
        IReadOnlyList<AtlasRegion> regions)
    {
        ValidateTexture(
            texture,
            textureDescription);

        ArgumentNullException.ThrowIfNull(regions);

        if (regions.Count == 0)
        {
            throw new ArgumentException(
                "Atlas must contain at least one region.",
                nameof(regions));
        }

        Texture = texture;

        TileWidth = 0;
        TileHeight = 0;

        Columns = 0;
        Rows = 0;

        _regions = new AtlasRegion[regions.Count];

        for (var i = 0; i < regions.Count; i++)
        {
            var region = regions[i];

            ValidateRegion(
                region,
                i);

            _regions[i] = region;
        }

        _count = _regions.Length;
        _isGrid = false;
    }

    public TextureHandle Texture { get; }

    /// <summary>
    /// Tile width for grid atlases.
    /// Returns zero for explicitly defined atlases.
    /// </summary>
    public int TileWidth { get; }

    /// <summary>
    /// Tile height for grid atlases.
    /// Returns zero for explicitly defined atlases.
    /// </summary>
    public int TileHeight { get; }

    /// <summary>
    /// Number of columns for grid atlases.
    /// Returns zero for explicitly defined atlases.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Number of rows for grid atlases.
    /// Returns zero for explicitly defined atlases.
    /// </summary>
    public int Rows { get; }

    public int Count => _count;

    public bool IsGrid => _isGrid;

    /// <summary>
    /// Gets a region by its index.
    /// Works with both grid and explicit atlases.
    /// </summary>
    public AtlasRegion GetRegion(int index)
    {
        if ((uint)index >= (uint)_count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        if (_regions is not null)
        {
            return _regions[index];
        }

        var column = index % Columns;
        var row = index / Columns;

        return GetGridRegion(
            column,
            row);
    }

    /// <summary>
    /// Gets a region by grid coordinates.
    /// Available only for regular grid atlases.
    /// </summary>
    public AtlasRegion GetRegion(
        int column,
        int row)
    {
        if (!_isGrid)
        {
            throw new InvalidOperationException(
                "Column and row access is available only for grid atlases.");
        }

        if ((uint)column >= (uint)Columns)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column));
        }

        if ((uint)row >= (uint)Rows)
        {
            throw new ArgumentOutOfRangeException(
                nameof(row));
        }

        return GetGridRegion(
            column,
            row);
    }

    private AtlasRegion GetGridRegion(
        int column,
        int row)
    {
        var uvWidth = 1.0f / Columns;
        var uvHeight = 1.0f / Rows;

        return new AtlasRegion(
            new Rectangle(
                column * uvWidth,
                row * uvHeight,
                uvWidth,
                uvHeight));
    }

    private static void ValidateTexture(
        TextureHandle texture,
        TextureDescription description)
    {
        if (!texture.IsValid)
        {
            throw new ArgumentException(
                "Texture handle is invalid.",
                nameof(texture));
        }

        if (description.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(description),
                "Texture width must be greater than zero.");
        }

        if (description.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(description),
                "Texture height must be greater than zero.");
        }
    }

    private static void ValidateRegion(
        AtlasRegion region,
        int index)
    {
        var uv = region.UV;

        if (!float.IsFinite(uv.X) ||
            !float.IsFinite(uv.Y) ||
            !float.IsFinite(uv.Width) ||
            !float.IsFinite(uv.Height))
        {
            throw new ArgumentException(
                $"Atlas region {index} contains non-finite coordinates.");
        }

        if (uv.X < 0.0f ||
            uv.Y < 0.0f ||
            uv.Width <= 0.0f ||
            uv.Height <= 0.0f)
        {
            throw new ArgumentException(
                $"Atlas region {index} has invalid dimensions.");
        }

        if (uv.X + uv.Width > 1.0f ||
            uv.Y + uv.Height > 1.0f)
        {
            throw new ArgumentException(
                $"Atlas region {index} exceeds texture boundaries.");
        }
    }
}