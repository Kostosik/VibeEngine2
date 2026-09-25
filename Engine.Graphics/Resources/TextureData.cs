namespace Engine.Graphics.Resources;

public sealed class TextureData
{
    public int Width { get; }

    public int Height { get; }

    public TextureFormat Format { get; }

    public ReadOnlyMemory<byte> Pixels { get; }

    public TextureData(
        int width,
        int height,
        TextureFormat format,
        ReadOnlyMemory<byte> pixels)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        Format = format;
        Pixels = pixels;
    }

    public TextureDescription Description =>
        new(
            Width,
            Height,
            Format);
}