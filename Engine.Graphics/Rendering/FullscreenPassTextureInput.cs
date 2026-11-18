using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Describes a named texture input of a fullscreen pass.
/// The resource must be declared as Read in the owning render-graph pass.
/// </summary>
public readonly record struct FullscreenPassTextureInput
{
    public FullscreenPassTextureInput(
        string name,
        RenderGraphResourceHandle resource,
        TextureSamplerDescription? sampler = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!resource.IsValid)
        {
            throw new ArgumentException(
                "A valid render-graph resource is required.",
                nameof(resource));
        }

        Name = name;
        Resource = resource;
        Sampler = sampler ?? TextureSamplerDescription.Default;
    }

    public string Name { get; }

    public RenderGraphResourceHandle Resource { get; }

    public TextureSamplerDescription Sampler { get; }
}