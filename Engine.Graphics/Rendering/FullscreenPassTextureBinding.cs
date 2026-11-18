using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Resolved texture binding passed to a fullscreen backend.
/// Contains a physical texture obtained through RenderGraphContext.
/// </summary>
public readonly record struct FullscreenPassTextureBinding
{
    public FullscreenPassTextureBinding(
        string name,
        TextureHandle texture,
        TextureSamplerDescription sampler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!texture.IsValid)
        {
            throw new ArgumentException(
                "A valid texture handle is required.",
                nameof(texture));
        }

        Name = name;
        Texture = texture;
        Sampler = sampler;
    }

    public string Name { get; }

    public TextureHandle Texture { get; }

    public TextureSamplerDescription Sampler { get; }
}