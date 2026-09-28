namespace Engine.Graphics.Resources;

public readonly record struct RenderTargetDescription(
    int Width,
    int Height,
    TextureFormat Format)
{
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
    }
}