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

        var bytesPerPixel = format switch
        {
            TextureFormat.Rgba8 => 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported texture format.")
        };

        var expectedSize =
            (long)width * height * bytesPerPixel;

        if (pixels.Length != expectedSize)
        {
            throw new ArgumentException(
                $"Invalid pixel data size. Expected {expectedSize} bytes, " +
                $"but received {pixels.Length} bytes.",
                nameof(pixels));
        }

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