using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Describes the properties required by a logical render-graph texture.
/// </summary>
public readonly record struct RenderGraphResourceDescriptor(
    int Width,
    int Height,
    TextureFormat Format = TextureFormat.Rgba8)
{
    public void Validate()
    {
        new RenderTargetDescription(
            Width,
            Height,
            Format).Validate();
    }

    public RenderTargetDescription ToRenderTargetDescription()
    {
        Validate();

        return new RenderTargetDescription(
            Width,
            Height,
            Format);
    }

    public static RenderGraphResourceDescriptor From(
        RenderTargetDescription description)
    {
        description.Validate();

        return new RenderGraphResourceDescriptor(
            description.Width,
            description.Height,
            description.Format);
    }
}
