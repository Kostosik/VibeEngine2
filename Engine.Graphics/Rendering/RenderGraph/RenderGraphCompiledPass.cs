namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// A pass retained by the compiler and arranged in execution order.
/// Dependencies contains the direct prerequisites of this pass.
/// </summary>
public sealed class RenderGraphCompiledPass
{
    internal RenderGraphCompiledPass(
        RenderGraphPassHandle handle,
        string name,
        RenderState state,
        RenderPassLayerRange layers,
        IReadOnlyList<RenderGraphResourceAccess> resourceAccesses,
        IReadOnlyList<RenderGraphPassHandle> dependencies,
        Action<RenderGraphContext>? execute)
    {
        Handle = handle;
        Name = name;
        State = state;
        Layers = layers;
        ResourceAccesses = Array.AsReadOnly(resourceAccesses.ToArray());
        Dependencies = Array.AsReadOnly(dependencies.ToArray());
        Execute = execute;
    }

    public RenderGraphPassHandle Handle { get; }

    public string Name { get; }

    public RenderState State { get; }

    public RenderPassLayerRange Layers { get; }

    public IReadOnlyList<RenderGraphResourceAccess> ResourceAccesses { get; }

    public IReadOnlyList<RenderGraphPassHandle> Dependencies { get; }

    internal Action<RenderGraphContext>? Execute { get; }
}
