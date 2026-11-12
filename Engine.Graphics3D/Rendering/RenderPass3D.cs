using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics3D.Lighting;

namespace Engine.Graphics3D.Rendering;

public static class RenderPass3D
{
    public static RenderPass World =>
        CreateWorld(SceneLighting3D.Default);

    public static RenderPass CreateWorld(
        SceneLighting3D lighting)
    {
        ArgumentNullException.ThrowIfNull(lighting);

        return new RenderPass(
            Name: "World3D",
            Target: RenderTargetHandle.Invalid,
            State: new RenderState(
                RenderBlendMode.Disabled,
                DepthTestEnabled: true,
                RenderCullMode.Back),
            ClearColor: true,
            Layers: new RenderPassLayerRange(
                RenderLayers.World,
                RenderLayers.Present - 1))
        {
            IsPostProcessSource = true,
            ContextData = lighting
        };
    }
}