namespace Engine.Graphics.Resources;

public sealed class TextureAtlas
{
    public TextureAtlas(
        TextureHandle texture,
        TextureDescription textureDescription,
        int tileWidth,
        int tileHeight)
    {
        if (!texture.IsValid)
        {
            throw new ArgumentException(
                "Texture handle is invalid.",
                nameof(texture));
        }

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
            textureDescription.Width /
            tileWidth;

        Rows =
            textureDescription.Height /
            tileHeight;
    }

    public TextureHandle Texture { get; }

    public int TileWidth { get; }

    public int TileHeight { get; }

    public int Columns { get; }

    public int Rows { get; }

    public int Count =>
        Columns * Rows;

    public AtlasRegion GetRegion(
        int index)
    {
        if ((uint)index >=
            (uint)Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        var x =
            index % Columns;

        var y =
            index / Columns;

        return GetRegion(
            x,
            y);
    }

    public AtlasRegion GetRegion(
        int column,
        int row)
    {
        if ((uint)column >=
            (uint)Columns)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column));
        }

        if ((uint)row >=
            (uint)Rows)
        {
            throw new ArgumentOutOfRangeException(
                nameof(row));
        }

        var uvWidth =
            1.0f / Columns;

        var uvHeight =
            1.0f / Rows;

        return new AtlasRegion(
            new Engine.Core.Math.Rectangle(
                column * uvWidth,
                row * uvHeight,
                uvWidth,
                uvHeight));
    }
}