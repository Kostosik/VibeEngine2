namespace Engine.Graphics.Resources;

public sealed class TextureAtlasBuilder
{
    private readonly record struct Placement(
        int X,
        int Y,
        int Width,
        int Height);

    private readonly record struct IndexedSprite(
        TextureAtlasSprite Sprite,
        int OriginalIndex);

    public TextureAtlasBuildResult Build(
        IReadOnlyList<TextureAtlasSprite> sprites,
        TextureAtlasBuilderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sprites);

        options ??= new TextureAtlasBuilderOptions();

        ValidateOptions(options);

        if (sprites.Count == 0)
        {
            throw new ArgumentException(
                "At least one sprite is required.",
                nameof(sprites));
        }

        if (sprites[0] is null)
        {
            throw new ArgumentException(
                "Sprite at index 0 is null.",
                nameof(sprites));
        }

        ArgumentNullException.ThrowIfNull(sprites[0].Texture);

        var format = sprites[0].Texture.Format;

        var indexedSprites = new IndexedSprite[sprites.Count];
        var names = new HashSet<string>(StringComparer.Ordinal);

        var border = options.Padding;
        var allocatedWidths = new int[sprites.Count];
        var allocatedHeights = new int[sprites.Count];

        for (var i = 0; i < sprites.Count; i++)
        {
            var sprite = sprites[i];

            if (sprite is null)
            {
                throw new ArgumentException(
                    $"Sprite at index {i} is null.",
                    nameof(sprites));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(sprite.Name);
            ArgumentNullException.ThrowIfNull(sprite.Texture);

            if (!names.Add(sprite.Name))
            {
                throw new ArgumentException(
                    $"Duplicate sprite name '{sprite.Name}'.",
                    nameof(sprites));
            }

            if (sprite.Texture.Format != format)
            {
                throw new ArgumentException(
                    "All sprites must use the same texture format.",
                    nameof(sprites));
            }

            var allocatedWidth = checked(
                sprite.Texture.Width + border * 2);

            var allocatedHeight = checked(
                sprite.Texture.Height + border * 2);

            if (allocatedWidth > options.MaxWidth ||
                allocatedHeight > options.MaxHeight)
            {
                throw new InvalidOperationException(
                    $"Sprite '{sprite.Name}' exceeds atlas dimensions.");
            }

            allocatedWidths[i] = allocatedWidth;
            allocatedHeights[i] = allocatedHeight;

            indexedSprites[i] = new IndexedSprite(
                sprite,
                i);
        }

        Array.Sort(
            indexedSprites,
            static (a, b) =>
            {
                var heightComparison =
                    b.Sprite.Texture.Height.CompareTo(
                        a.Sprite.Texture.Height);

                if (heightComparison != 0)
                    return heightComparison;

                var widthComparison =
                    b.Sprite.Texture.Width.CompareTo(
                        a.Sprite.Texture.Width);

                return widthComparison != 0
                    ? widthComparison
                    : a.OriginalIndex.CompareTo(b.OriginalIndex);
            });

        var placements = new Placement[sprites.Count];

        var currentX = 0;
        var currentY = 0;
        var rowHeight = 0;

        var usedWidth = 0;
        var usedHeight = 0;

        foreach (var indexed in indexedSprites)
        {
            var index = indexed.OriginalIndex;

            var width = allocatedWidths[index];
            var height = allocatedHeights[index];

            if (currentX + width > options.MaxWidth)
            {
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }

            if (currentY + height > options.MaxHeight)
            {
                throw new InvalidOperationException(
                    "Sprites do not fit into the configured atlas dimensions.");
            }

            placements[index] = new Placement(
                currentX + border,
                currentY + border,
                indexed.Sprite.Texture.Width,
                indexed.Sprite.Texture.Height);

            currentX += width;

            rowHeight = Math.Max(rowHeight, height);

            usedWidth = Math.Max(usedWidth, currentX);
            usedHeight = Math.Max(usedHeight, currentY + height);
        }

        var bytesPerPixel = format switch
        {
            TextureFormat.Rgba8 => 4,

            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported texture format.")
        };

        var atlasPixels = new byte[
            checked(usedWidth * usedHeight * bytesPerPixel)];

        var regions = new Dictionary<string, AtlasRegion>(
            sprites.Count,
            StringComparer.Ordinal);

        var orderedRegions = new AtlasRegion[sprites.Count];

        var description = new TextureDescription(
            usedWidth,
            usedHeight,
            format);

        for (var i = 0; i < sprites.Count; i++)
        {
            var sprite = sprites[i];
            var placement = placements[i];

            CopyPixels(
                sprite.Texture,
                atlasPixels,
                usedWidth,
                bytesPerPixel,
                placement);

            if (options.Extrusion > 0)
            {
                ExtrudePixels(
                    atlasPixels,
                    usedWidth,
                    usedHeight,
                    bytesPerPixel,
                    placement,
                    options.Extrusion);
            }

            var region = AtlasRegion.FromPixels(
                placement.X,
                placement.Y,
                placement.Width,
                placement.Height,
                description);

            regions.Add(sprite.Name, region);
            orderedRegions[i] = region;
        }

        var atlasTexture = new TextureData(
            usedWidth,
            usedHeight,
            format,
            atlasPixels);

        return new TextureAtlasBuildResult(
            atlasTexture,
            regions,
            orderedRegions);
    }

    private static void ValidateOptions(
        TextureAtlasBuilderOptions options)
    {
        if (options.MaxWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaxWidth));
        }

        if (options.MaxHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaxHeight));
        }

        if (options.Padding < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.Padding));
        }

        if (options.Extrusion < 0 ||
            options.Extrusion > options.Padding)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.Extrusion),
                "Extrusion must be between zero and Padding.");
        }
    }

    private static void CopyPixels(
        TextureData source,
        byte[] destination,
        int destinationWidth,
        int bytesPerPixel,
        Placement placement)
    {
        var sourcePixels = source.Pixels.Span;

        var sourceStride = checked(
            source.Width * bytesPerPixel);

        var destinationStride = checked(
            destinationWidth * bytesPerPixel);

        var rowLength = checked(
            placement.Width * bytesPerPixel);

        for (var row = 0; row < placement.Height; row++)
        {
            var sourceOffset = row * sourceStride;

            var destinationOffset =
                (placement.Y + row) * destinationStride +
                placement.X * bytesPerPixel;

            sourcePixels
                .Slice(sourceOffset, rowLength)
                .CopyTo(
                    destination.AsSpan(
                        destinationOffset,
                        rowLength));
        }
    }

    private static void ExtrudePixels(
        byte[] pixels,
        int atlasWidth,
        int atlasHeight,
        int bytesPerPixel,
        Placement placement,
        int extrusion)
    {
        var stride = checked(atlasWidth * bytesPerPixel);

        var left = placement.X - extrusion;
        var top = placement.Y - extrusion;

        var right = placement.X + placement.Width - 1 + extrusion;
        var bottom = placement.Y + placement.Height - 1 + extrusion;

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                if (x >= placement.X &&
                    x < placement.X + placement.Width &&
                    y >= placement.Y &&
                    y < placement.Y + placement.Height)
                {
                    continue;
                }

                var sourceX = Math.Clamp(
                    x,
                    placement.X,
                    placement.X + placement.Width - 1);

                var sourceY = Math.Clamp(
                    y,
                    placement.Y,
                    placement.Y + placement.Height - 1);

                var sourceOffset =
                    sourceY * stride + sourceX * bytesPerPixel;

                var destinationOffset =
                    y * stride + x * bytesPerPixel;

                pixels.AsSpan(
                        sourceOffset,
                        bytesPerPixel)
                    .CopyTo(
                        pixels.AsSpan(
                            destinationOffset,
                            bytesPerPixel));
            }
        }
    }
}