
namespace Engine.Graphics.Resources;

public readonly record struct RenderTargetDescription(
    int Width,
    int Height,
    TextureFormat Format)
{
    public TextureColorSpace ColorSpace { get; init; }
    = TextureColorSpace.Linear;
    public bool HasDepthAttachment { get; init; } = true;

    public RenderTargetDescription(
        int width,
        int height,
        TextureFormat format,
        bool hasDepthAttachment)
        : this(
            width,
            height,
            format)
    {
        HasDepthAttachment = hasDepthAttachment;
    }

    public RenderTargetDescription(
        int width,
        int height)
        : this(
            width,
            height,
            TextureFormat.Rgba8)
    {
    }

    public void Validate()
    {
        if (!Enum.IsDefined(ColorSpace))
        {
            throw new ArgumentOutOfRangeException(
                nameof(ColorSpace));
        }

        if (ColorSpace == TextureColorSpace.Srgb &&
            Format != TextureFormat.Rgba8)
        {
            throw new ArgumentException(
                "sRGB render targets support only Rgba8.",
                nameof(ColorSpace));
        }
        if (Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Width));
        }

        if (Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Height));
        }

        if (!Enum.IsDefined(Format))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Format),
                Format,
                "Unsupported render target texture format.");
        }
    }
}
