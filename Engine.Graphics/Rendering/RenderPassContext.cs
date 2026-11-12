using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public readonly record struct RenderPassContext(
    RenderTargetHandle Target,
    RenderState State,
    bool ClearColor)
{
    public IRenderPassContextData? ContextData { get; init; }
    public bool IsPostProcessSource { get; init; }
}