namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Identifies a pass inside one render graph.
/// </summary>
public readonly record struct RenderGraphPassHandle(
    Guid GraphId,
    int Index)
{
    public bool IsValid =>
        GraphId != Guid.Empty && Index >= 0;

    public static RenderGraphPassHandle Invalid =>
        default;
}
