using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public readonly record struct RenderPassContext(
    RenderTargetHandle Target,
    RenderState State,
    bool ClearColor);