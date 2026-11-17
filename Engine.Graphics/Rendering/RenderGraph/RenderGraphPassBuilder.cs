namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Declares the resource accesses and state of one render-graph pass.
/// Instances are created by <see cref="RenderGraphBuilder.AddPass"/>.
/// </summary>
public sealed class RenderGraphPassBuilder
{
    private readonly Guid _graphId;
    private readonly int _passIndex;
    private readonly List<RenderGraphResourceAccess> _resourceAccesses = new();
    private readonly List<RenderGraphPassHandle> _explicitDependencies = new();
    private RenderGraphDepthAttachment? _depthAttachment;
    private bool _executeQueuedCommands = true;
    private bool _isPostProcessSource;
    private bool _hasSideEffects;
    private Action<RenderGraphContext>? _execute;
    private bool _built;
    private IRenderPassContextData? _contextData;
    internal RenderGraphPassBuilder(
        Guid graphId,
        int passIndex,
        string name)
    {
        _graphId = graphId;
        _passIndex = passIndex;
        Name = name;
    }

    public string Name { get; }

    public RenderState State { get; private set; } =
        RenderState.Default;

    public RenderPassLayerRange Layers { get; private set; } =
        RenderPassLayerRange.All;

    public RenderGraphPassBuilder SetExecuteQueuedCommands(
    bool enabled)
    {
        EnsureMutable();
        _executeQueuedCommands = enabled;
        return this;
    }

    /// <summary>
    /// Requests clearing the depth attachment of this pass.
    /// </summary>
    public RenderGraphPassBuilder SetClearDepth(bool enabled = true)
    {
        EnsureMutable();

        _depthAttachment = enabled
            ? new RenderGraphDepthAttachment(
                RenderGraphLoadAction.Clear,
                RenderGraphStoreAction.Store)
            : null;

        return this;
    }

    /// <summary>
    /// Sets the data passed to the render-pass executor when this pass begins.
    /// </summary>
    public RenderGraphPassBuilder SetContextData(
        IRenderPassContextData? contextData)
    {
        EnsureMutable();
        _contextData = contextData;
        return this;
    }

    /// <summary>
    /// Marks this pass as externally observable.
    /// The compiler will preserve it even when it does not contribute to graph outputs.
    /// </summary>
    public RenderGraphPassBuilder SetSideEffect()
    {
        EnsureMutable();
        _hasSideEffects = true;
        return this;
    }

    public RenderGraphPassBuilder SetPostProcessSource()
    {
        EnsureMutable();
        _isPostProcessSource = true;
        return this;
    }

    /// <summary>
    /// Declares that this pass samples or otherwise reads a resource.
    /// </summary>
    public RenderGraphPassBuilder Read(
        RenderGraphResourceHandle resource)
    {
        EnsureMutable();
        ValidateResourceHandle(resource);
        EnsureResourceIsNotAlreadyUsed(resource);

        _resourceAccesses.Add(
            new RenderGraphResourceAccess(
                resource,
                RenderGraphResourceAccessKind.Read,
                RenderGraphLoadAction.DontCare,
                RenderGraphStoreAction.Store));

        return this;
    }

    /// <summary>
    /// Declares the color attachment written by this pass.
    /// The current backend supports one color output per pass.
    /// </summary>
    public RenderGraphPassBuilder WriteColor(
        RenderGraphResourceHandle resource,
        RenderGraphLoadAction loadAction = RenderGraphLoadAction.Clear,
        RenderGraphStoreAction storeAction = RenderGraphStoreAction.Store)
    {
        EnsureMutable();
        ValidateResourceHandle(resource);

        if (!Enum.IsDefined(loadAction))
        {
            throw new ArgumentOutOfRangeException(
                nameof(loadAction),
                loadAction,
                "Unsupported render-graph load action.");
        }
        if (!Enum.IsDefined(storeAction))
        {
            throw new ArgumentOutOfRangeException(
                nameof(storeAction),
                storeAction,
                "Unsupported render-graph store action.");
        }
        EnsureResourceIsNotAlreadyUsed(resource);

        if (_resourceAccesses.Any(
                access => access.Kind == RenderGraphResourceAccessKind.WriteColor))
        {
            throw new InvalidOperationException(
                "A render-graph pass can declare only one color output in the current version.");
        }

        _resourceAccesses.Add(
            new RenderGraphResourceAccess(
                resource,
                RenderGraphResourceAccessKind.WriteColor,
                loadAction,
                storeAction));

        return this;
    }

    /// <summary>
    /// Adds an explicit ordering dependency on a pass that was declared earlier.
    /// Resource reads and writes should be preferred whenever they can express the dependency.
    /// </summary>
    public RenderGraphPassBuilder DependsOn(
        RenderGraphPassHandle pass)
    {
        EnsureMutable();

        if (!pass.IsValid ||
            pass.GraphId != _graphId ||
            pass.Index >= _passIndex)
        {
            throw new ArgumentException(
                "The dependency must reference a pass declared earlier in the same graph.",
                nameof(pass));
        }

        if (!_explicitDependencies.Contains(pass))
        {
            _explicitDependencies.Add(pass);
        }

        return this;
    }

    public RenderGraphPassBuilder SetRenderState(
        RenderState state)
    {
        EnsureMutable();
        State = state;
        return this;
    }

    public RenderGraphPassBuilder SetLayerRange(
        RenderPassLayerRange layers)
    {
        EnsureMutable();
        Layers = layers;
        return this;
    }

    /// <summary>
    /// Sets the callback invoked when this pass is executed.
    /// The callback may only access resources declared by this pass.
    /// </summary>
    public RenderGraphPassBuilder SetExecute(
        Action<RenderGraphContext> execute)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        return this;
    }

    internal RenderGraphPassDefinition Build(
        RenderGraphPassHandle handle)
    {
        EnsureMutable();

        if (_depthAttachment.HasValue &&
            !_resourceAccesses.Any(
                access => access.Kind == RenderGraphResourceAccessKind.WriteColor))
        {
            throw new InvalidOperationException(
                $"Pass '{Name}' cannot declare a depth attachment without a color output.");
        }

        _built = true;

        return new RenderGraphPassDefinition(
            handle,
            Name,
            State,
            Layers,
            Array.AsReadOnly(_resourceAccesses.ToArray()),
            Array.AsReadOnly(_explicitDependencies.ToArray()),
            _execute,
            _executeQueuedCommands,
            _isPostProcessSource,
            _contextData,
            _hasSideEffects, _depthAttachment);
    }

    /// <summary>
    /// Declares the depth attachment of this pass.
    /// It is attached to the same render target as the color output.
    /// </summary>
    public RenderGraphPassBuilder SetDepthAttachment(
        RenderGraphLoadAction loadAction = RenderGraphLoadAction.Clear,
        RenderGraphStoreAction storeAction = RenderGraphStoreAction.Store)
    {
        EnsureMutable();

        if (!Enum.IsDefined(loadAction))
        {
            throw new ArgumentOutOfRangeException(nameof(loadAction));
        }

        if (!Enum.IsDefined(storeAction))
        {
            throw new ArgumentOutOfRangeException(nameof(storeAction));
        }

        _depthAttachment = new RenderGraphDepthAttachment(
            loadAction,
            storeAction);

        return this;
    }

    private void ValidateResourceHandle(
        RenderGraphResourceHandle resource)
    {
        if (!resource.IsValid || resource.GraphId != _graphId)
        {
            throw new ArgumentException(
                "The resource must be a valid resource from the same render graph.",
                nameof(resource));
        }
    }

    private void EnsureResourceIsNotAlreadyUsed(
        RenderGraphResourceHandle resource)
    {
        if (_resourceAccesses.Any(access => access.Resource == resource))
        {
            throw new InvalidOperationException(
                $"Pass '{Name}' declares resource '{resource.Index}' more than once. " +
                "Reading and writing the same resource in one pass is not supported.");
        }
    }

    private void EnsureMutable()
    {
        ObjectDisposedException.ThrowIf(_built, this);
    }
}
