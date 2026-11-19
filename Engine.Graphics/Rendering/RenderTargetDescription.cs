
namespace Engine.Graphics.Resources;

public readonly record struct RenderTargetDescription(
    int Width,
    int Height,
    TextureFormat Format)
{
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
