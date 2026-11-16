namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Immutable declarative description of a render graph.
/// Compile it before execution to validate dependencies and produce an execution plan.
/// </summary>
public sealed class RenderGraph
{
    private readonly IReadOnlyList<RenderGraphResourceDefinition> _resources;
    private readonly IReadOnlyList<RenderGraphPassDefinition> _passes;

    internal RenderGraph(
        Guid graphId,
        IReadOnlyList<RenderGraphResourceDefinition> resources,
        IReadOnlyList<RenderGraphPassDefinition> passes,
        IReadOnlyList<RenderGraphResourceHandle> outputs)
    {
        GraphId = graphId;

        _resources = Array.AsReadOnly(resources.ToArray());
        _passes = Array.AsReadOnly(passes.ToArray());
        Outputs = Array.AsReadOnly(outputs.ToArray());
    }

    internal Guid GraphId { get; }

    internal IReadOnlyList<RenderGraphResourceDefinition> ResourceDefinitions =>
        _resources;

    internal IReadOnlyList<RenderGraphPassDefinition> PassDefinitions =>
        _passes;

    public IReadOnlyList<RenderGraphResourceHandle> Outputs { get; }

    public int ResourceCount =>
        _resources.Count;

    public int PassCount =>
        _passes.Count;
}
