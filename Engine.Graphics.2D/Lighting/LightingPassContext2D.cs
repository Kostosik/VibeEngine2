using Engine.Graphics.Rendering.RenderGraph;

namespace Engine.Graphics2D.Lighting;

public readonly record struct LightingPassContext2D(
    LightingChannel2D Channel,
    LightingContext2D LightingContext,
    LightingResult2D LightingResult,
    RenderGraphResourceHandle SceneColor,
    RenderGraphResourceHandle NormalBuffer,
    RenderGraphResourceHandle Output)
{
    public IReadOnlyList<ILightingContribution2D> Contributions =>
        Channel switch
        {
            LightingChannel2D.Direct =>
                LightingResult.DirectLighting,

            LightingChannel2D.Ambient =>
                LightingResult.AmbientLighting,

            LightingChannel2D.Indirect =>
                LightingResult.IndirectLighting,

            LightingChannel2D.Emission =>
                LightingResult.Emission,

            _ => throw new InvalidOperationException(
                $"Unsupported lighting channel: {Channel}.")
        };
}