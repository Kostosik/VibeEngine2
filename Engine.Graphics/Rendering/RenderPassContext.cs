using Engine.Graphics.Resources;
using Engine.Core.Math;

namespace Engine.Graphics.Rendering;

public readonly record struct RenderPassContext(
    RenderTargetHandle Target,
    RenderState State,
    bool ClearColor)
{
    public IRenderPassContextData? ContextData { get; init; }
    public bool IsPostProcessSource { get; init; }
    public bool ClearDepth { get; init; }
    public Vector4 ClearColorValue { get; init; } =
    Vector4.Zero;
}