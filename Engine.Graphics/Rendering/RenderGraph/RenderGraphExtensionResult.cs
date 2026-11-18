namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Describes the result of a render-graph extension.
/// </summary>
public readonly record struct RenderGraphExtensionResult(
    RenderGraphPassHandle? LastPass,
    RenderGraphResourceHandle? Output)
{
    /// <summary>
    /// Indicates that the extension did not modify the graph.
    /// </summary>
    public static RenderGraphExtensionResult Unchanged =>
        new(null, null);
}