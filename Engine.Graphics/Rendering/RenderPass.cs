using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public readonly record struct RenderPass(
    string Name,
    RenderTargetHandle Target,
    RenderState State,
    bool ClearColor,
    RenderPassLayerRange Layers);