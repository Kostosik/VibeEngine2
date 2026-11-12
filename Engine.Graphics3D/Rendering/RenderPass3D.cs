
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics3D.Rendering;

public static class RenderPass3D
{
    public static RenderPass World =>
        new(
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
            IsPostProcessSource = true
        };
}
