namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Identifies a logical resource inside one render graph.
/// The graph identifier prevents accidentally mixing handles from different graphs.
/// </summary>
public readonly record struct RenderGraphResourceHandle(
    Guid GraphId,
    int Index)
{
    public bool IsValid =>
        GraphId != Guid.Empty && Index >= 0;

    public static RenderGraphResourceHandle Invalid =>
        default;
}
