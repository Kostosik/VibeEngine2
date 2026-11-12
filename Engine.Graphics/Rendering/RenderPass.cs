using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public readonly record struct RenderPass(
    string Name,
    RenderTargetHandle Target,
    RenderState State,
    bool ClearColor,
    RenderPassLayerRange Layers)
{
    public IRenderPassContextData? ContextData { get; init; }
    /// <summary>
    /// Indicates that this pass produces the image
    /// which will be sent through post-processing.
    /// </summary>
    public bool IsPostProcessSource { get; init; }
}


