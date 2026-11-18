using Engine.Graphics.Commands;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Provides a render-graph pass with access to its declared inputs and output.
/// Resource access is validated against the pass declarations.
/// </summary>
public sealed class RenderGraphContext
{
    private readonly IReadOnlyDictionary<RenderGraphResourceHandle, RenderGraphCompiledResource> _resources;
    private readonly IReadOnlyDictionary<RenderGraphResourceHandle, RenderTargetHandle> _physicalTargets;
    private readonly IReadOnlyList<RenderGraphResourceAccess> _accesses;
    private readonly IRenderTargetManager _renderTargets;
    private readonly IRenderPassExecutor _passExecutor;
    private readonly bool _canSubmitCommands;
    private readonly IFullscreenPassExecutor? _fullscreenPassExecutor;
    internal RenderGraphContext(
        string passName,
        RenderTargetHandle colorTarget,
        IReadOnlyList<RenderGraphResourceAccess> accesses,
        IReadOnlyDictionary<RenderGraphResourceHandle, RenderGraphCompiledResource> resources,
        IReadOnlyDictionary<RenderGraphResourceHandle, RenderTargetHandle> physicalTargets,
        IRenderTargetManager renderTargets,
        IRenderPassExecutor passExecutor,
        IFullscreenPassExecutor? fullscreenPassExecutor, bool canSubmitCommands)
    {
        PassName = passName;
        ColorTarget = colorTarget;
        _accesses = accesses;
        _resources = resources;
        _physicalTargets = physicalTargets;
        _renderTargets = renderTargets;
        _passExecutor = passExecutor;
        _canSubmitCommands = canSubmitCommands;
        _fullscreenPassExecutor = fullscreenPassExecutor;

    }

    public string PassName { get; }

    /// <summary>
    /// Gets the physical render target used as this pass' color output.
    /// An invalid handle represents the window backbuffer.
    /// </summary>
    public RenderTargetHandle ColorTarget { get; }

    public bool HasColorOutput => _canSubmitCommands;

    /// <summary>
    /// Resolves a declared read resource to its color texture.
    /// Backbuffers cannot be sampled through this API.
    /// </summary>
    public TextureHandle GetTexture(RenderGraphResourceHandle resource)
    {
        EnsureAccess(resource, RenderGraphResourceAccessKind.Read);

        var definition = _resources[resource];
        if (definition.Kind == RenderGraphResourceKind.Backbuffer)
        {
            throw new InvalidOperationException(
                $"Pass '{PassName}' cannot sample backbuffer resource '{definition.Name}'.");
        }

        var target = _physicalTargets[resource];
        if (!target.IsValid)
        {
            throw new InvalidOperationException(
                $"Resource '{definition.Name}' has no sampleable render target.");
        }

        return _renderTargets.GetColorTexture(target);
    }

    /// <summary>
    /// Executes a fullscreen operation using declared graph resources.
    /// </summary>
    public void ExecuteFullscreen(
        FullscreenPassDescription pass,
        RenderGraphResourceHandle source)
    {
        ArgumentNullException.ThrowIfNull(pass);

        EnsureCanSubmitCommands();

        var executor = _fullscreenPassExecutor
            ?? throw new NotSupportedException(
                "The render pass executor does not support fullscreen passes.");

        var sourceTexture = GetTexture(source);

        var outputAccess = _accesses.Single(
            access => access.Kind == RenderGraphResourceAccessKind.WriteColor);

        // Prevent framebuffer feedback when two graph resources
        // reference the same physical render target.
        if (ColorTarget.IsValid &&
            _physicalTargets[source] == ColorTarget)
        {
            throw new InvalidOperationException(
                "A fullscreen pass cannot sample its own output render target.");
        }

        var outputDescription = _resources[outputAccess.Resource]
            .Descriptor
            .ToRenderTargetDescription();

        executor.ExecuteFullscreen(
            pass,
            sourceTexture,
            outputDescription);
    }

    /// <summary>
    /// Submits a graphics command to the currently active output pass.
    /// </summary>
    public void Execute(IRenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureCanSubmitCommands();
        _passExecutor.Execute(command);
    }

    /// <summary>
    /// Submits world texture data to the currently active output pass.
    /// </summary>
    public void Execute(DrawWorldTextureData command)
    {
        EnsureCanSubmitCommands();
        _passExecutor.Execute(command);
    }

    private void EnsureAccess(
        RenderGraphResourceHandle resource,
        RenderGraphResourceAccessKind requiredKind)
    {
        if (!_resources.ContainsKey(resource))
        {
            throw new ArgumentException(
                "The resource does not belong to this compiled render graph.",
                nameof(resource));
        }

        if (!_accesses.Any(access =>
                access.Resource == resource && access.Kind == requiredKind))
        {
            throw new InvalidOperationException(
                $"Pass '{PassName}' did not declare {requiredKind} access to resource '{_resources[resource].Name}'.");
        }
    }

    private void EnsureCanSubmitCommands()
    {
        if (!_canSubmitCommands)
        {
            throw new InvalidOperationException(
                $"Pass '{PassName}' has no declared color output and cannot submit draw commands.");
        }
    }
}
