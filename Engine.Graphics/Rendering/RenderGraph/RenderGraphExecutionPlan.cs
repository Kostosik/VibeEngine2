namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Immutable result of compiling a render graph.
/// Passes are in a valid topological execution order.
/// </summary>
public sealed class RenderGraphExecutionPlan
{
    internal RenderGraphExecutionPlan(
        Guid graphId,
        IReadOnlyList<RenderGraphCompiledPass> passes,
        IReadOnlyList<RenderGraphCompiledResource> resources,
        IReadOnlyList<RenderGraphResourceHandle> outputs,
        int culledPassCount)
    {
        GraphId = graphId;
        Passes = Array.AsReadOnly(passes.ToArray());
        Resources = Array.AsReadOnly(resources.ToArray());
        Outputs = Array.AsReadOnly(outputs.ToArray());
        CulledPassCount = culledPassCount;
    }

    public Guid GraphId { get; }

    public IReadOnlyList<RenderGraphCompiledPass> Passes { get; }

    public IReadOnlyList<RenderGraphCompiledResource> Resources { get; }

    public IReadOnlyList<RenderGraphResourceHandle> Outputs { get; }

    public int CulledPassCount { get; }
}
