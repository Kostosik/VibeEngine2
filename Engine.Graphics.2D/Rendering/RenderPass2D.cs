using Engine.Graphics.Resources;
using Engine.Graphics.Rendering;

namespace Engine.Graphics2D.Rendering;

public static class RenderPass2D
{
    public static RenderPass Default =>
    new(
        Name: "Main",
        Target: RenderTargetHandle.Invalid,
        State: RenderState.Default2D,
        ClearColor: true,
        Layers: RenderPassLayerRange.All);

    public static RenderPass World =>
        new(
            Name: "World",
            Target: RenderTargetHandle.Invalid,
            State: RenderState.Default2D,
            ClearColor: true,
            Layers: new RenderPassLayerRange(
                RenderLayers.World,
                RenderLayers.Present - 1));

    public static RenderPass Ui =>
        new(
            Name: "UI",
            Target: RenderTargetHandle.Invalid,
            State: RenderState.Default2D,
            ClearColor: false,
            Layers: new RenderPassLayerRange(
                RenderLayers.Ui,
                RenderLayers.Debug - 1));

    public static RenderPass Debug =>
        new(
            Name: "Debug",
            Target: RenderTargetHandle.Invalid,
            State: RenderState.Default2D,
            ClearColor: false,
            Layers: new RenderPassLayerRange(
                RenderLayers.Debug,
                int.MaxValue));

    public static RenderPass Present =>
        new(
            Name: "Present",
            Target: RenderTargetHandle.Invalid,
            State: RenderState.Default2D,
            ClearColor: false,
            Layers: new RenderPassLayerRange(
                RenderLayers.Present,
                RenderLayers.Ui - 1));
}