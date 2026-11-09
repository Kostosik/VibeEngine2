namespace Engine.Graphics.Resources;

public sealed class TextureData
{
    public TextureDescription Description { get; }

    public int Width => Description.Width;

    public int Height => Description.Height;

    public TextureFormat Format => Description.Format;

    public ReadOnlyMemory<byte> Pixels { get; }

    public TextureData(
        int width,
        int height,
        TextureFormat format,
        ReadOnlyMemory<byte> pixels)
        : this(
            new TextureDescription(width, height, format),
            pixels)
    {
    }

    public TextureData(
        TextureDescription description,
        ReadOnlyMemory<byte> pixels)
    {
        if (description.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(description));

        if (description.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(description));

        var bytesPerPixel = description.Format switch
        {
            TextureFormat.Rgba8 => 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(description),
                "Unsupported texture format.")
        };

        var expectedSize =
            (long)description.Width *
            description.Height *
            bytesPerPixel;

        if (pixels.Length != expectedSize)
        {
            throw new ArgumentException(
                $"Invalid pixel data size. Expected {expectedSize} bytes, " +
                $"but received {pixels.Length} bytes.",
                nameof(pixels));
        }

        Description = description;
        Pixels = pixels;
    }
}