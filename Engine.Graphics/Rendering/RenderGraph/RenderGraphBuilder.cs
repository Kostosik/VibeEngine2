using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Builds an immutable render-graph description.
/// No GPU resources are created by this class.
/// </summary>
public sealed class RenderGraphBuilder
{
    private readonly Guid _graphId = Guid.NewGuid();

    private readonly List<RenderGraphResourceDefinition> _resources = new();
    private readonly List<RenderGraphPassDefinition> _passes = new();
    private readonly List<RenderGraphResourceHandle> _outputs = new();

    private readonly HashSet<string> _resourceNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _passNames = new(StringComparer.Ordinal);

    private bool _built;

    /// <summary>
    /// Declares a graph-owned texture. Its physical render target is allocated later.
    /// </summary>
    public RenderGraphResourceHandle CreateTexture(
        string name,
        RenderGraphResourceDescriptor descriptor)
    {
        EnsureMutable();
        ValidateResourceName(name);
        descriptor.Validate();

        var handle = new RenderGraphResourceHandle(
            _graphId,
            _resources.Count);

        _resources.Add(
            new RenderGraphResourceDefinition(
                handle,
                name,
                RenderGraphResourceKind.Transient,
                descriptor,
                RenderTargetHandle.Invalid));

        _resourceNames.Add(name);

        return handle;
    }

    /// <summary>
    /// Imports a render target owned by the caller. The graph does not own its lifetime.
    /// </summary>
    public RenderGraphResourceHandle ImportRenderTarget(
        string name,
        RenderTargetHandle target,
        RenderTargetDescription description)
    {
        EnsureMutable();
        ValidateResourceName(name);

        if (!target.IsValid)
        {
            throw new ArgumentException(
                "A valid render target handle is required.",
                nameof(target));
        }

        var descriptor = RenderGraphResourceDescriptor.From(description);
        var handle = new RenderGraphResourceHandle(
            _graphId,
            _resources.Count);

        _resources.Add(
            new RenderGraphResourceDefinition(
                handle,
                name,
                RenderGraphResourceKind.ImportedRenderTarget,
                descriptor,
                target));

        _resourceNames.Add(name);

        return handle;
    }

    /// <summary>
    /// Imports the window backbuffer. The graph does not own its lifetime.
    /// </summary>
    public RenderGraphResourceHandle ImportBackbuffer(
        string name,
        RenderTargetDescription description)
    {
        EnsureMutable();
        ValidateResourceName(name);

        var descriptor = RenderGraphResourceDescriptor.From(description);
        var handle = new RenderGraphResourceHandle(
            _graphId,
            _resources.Count);

        _resources.Add(
            new RenderGraphResourceDefinition(
                handle,
                name,
                RenderGraphResourceKind.Backbuffer,
                descriptor,
                RenderTargetHandle.Invalid));

        _resourceNames.Add(name);

        return handle;
    }

    /// <summary>
    /// Declares and configures a pass.
    /// Resources may be declared before or after passes, but all declarations
    /// must exist before <see cref="Build"/> is called.
    /// </summary>
    public RenderGraphPassHandle AddPass(
        string name,
        Action<RenderGraphPassBuilder> configure)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        if (_passNames.Contains(name))
        {
            throw new ArgumentException(
                $"A render-graph pass named '{name}' already exists.",
                nameof(name));
        }

        var handle = new RenderGraphPassHandle(
            _graphId,
            _passes.Count);

        var passBuilder = new RenderGraphPassBuilder(
            _graphId,
            handle.Index,
            name);

        configure(passBuilder);

        var definition = passBuilder.Build(handle);

        _passes.Add(definition);
        _passNames.Add(name);

        return handle;
    }

    /// <summary>
    /// Marks a resource as an externally visible output of this graph.
    /// At least one output is required before building.
    /// </summary>
    public void SetOutput(
        RenderGraphResourceHandle resource)
    {
        EnsureMutable();
        ValidateResourceHandle(resource);

        if (!_outputs.Contains(resource))
        {
            _outputs.Add(resource);
        }
    }

    /// <summary>
    /// Creates an immutable graph description. This does not allocate GPU resources
    /// or execute any render pass.
    /// </summary>
    public RenderGraph Build()
    {
        EnsureMutable();

        if (_outputs.Count == 0)
        {
            throw new InvalidOperationException(
                "A render graph must declare at least one output resource.");
        }

        ValidateDeclarations();

        _built = true;

        return new RenderGraph(
            _graphId,
            _resources,
            _passes,
            _outputs);
    }

    private void ValidateDeclarations()
    {
        foreach (var pass in _passes)
        {
            foreach (var access in pass.ResourceAccesses)
            {
                ValidateResourceHandle(access.Resource);

                if (access.Kind == RenderGraphResourceAccessKind.WriteColor &&
                    access.LoadAction == RenderGraphLoadAction.Load)
                {
                    var resource = _resources[access.Resource.Index];

                    if (resource.Kind == RenderGraphResourceKind.Transient)
                    {
                        throw new InvalidOperationException(
                            $"Pass '{pass.Name}' cannot load transient resource " +
                            $"'{resource.Name}' before it has existing contents.");
                    }
                }
            }

            foreach (var dependency in pass.ExplicitDependencies)
            {
                if (!dependency.IsValid ||
                    dependency.GraphId != _graphId ||
                    dependency.Index >= pass.Handle.Index)
                {
                    throw new InvalidOperationException(
                        $"Pass '{pass.Name}' contains an invalid explicit dependency.");
                }
            }
        }

        foreach (var output in _outputs)
        {
            ValidateResourceHandle(output);
        }
    }

    private void ValidateResourceName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_resourceNames.Contains(name))
        {
            throw new ArgumentException(
                $"A render-graph resource named '{name}' already exists.",
                nameof(name));
        }
    }

    private void ValidateResourceHandle(
        RenderGraphResourceHandle resource)
    {
        if (!resource.IsValid ||
            resource.GraphId != _graphId ||
            resource.Index >= _resources.Count)
        {
            throw new ArgumentException(
                "The resource must be a valid resource from this render graph.",
                nameof(resource));
        }
    }

    private void EnsureMutable()
    {
        ObjectDisposedException.ThrowIf(_built, this);
    }
}
