using Engine.Core.Math;

namespace Engine.Graphics.Resources;

/// <summary>
/// Represents a rectangular region inside a texture atlas.
/// UV coordinates are normalized to the range [0..1].
/// </summary>
public readonly record struct AtlasRegion(
    Rectangle UV)
{
    /// <summary>
    /// Creates an atlas region from pixel coordinates.
    /// </summary>
    public static AtlasRegion FromPixels(
        int x,
        int y,
        int width,
        int height,
        TextureDescription textureDescription)
    {
        if (textureDescription.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(textureDescription),
                "Texture width must be greater than zero.");
        }

        if (textureDescription.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(textureDescription),
                "Texture height must be greater than zero.");
        }

        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x));
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(y));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height));
        }

        if ((long)x + width > textureDescription.Width)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Region exceeds texture width.");
        }

        if ((long)y + height > textureDescription.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                "Region exceeds texture height.");
        }

        return new AtlasRegion(
            new Rectangle(
                x / (float)textureDescription.Width,
                y / (float)textureDescription.Height,
                width / (float)textureDescription.Width,
                height / (float)textureDescription.Height));
    }
}