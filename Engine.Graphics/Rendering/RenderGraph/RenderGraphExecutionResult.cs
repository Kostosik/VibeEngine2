using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Holds externally visible graph outputs after execution.
/// Transient output targets remain alive until this result is disposed.
/// Imported targets remain owned by their original owner.
/// </summary>
public sealed class RenderGraphExecutionResult : IDisposable
{
    private readonly Guid _graphId;
    private readonly IRenderTargetManager _renderTargets;
    private readonly Dictionary<RenderGraphResourceHandle, RenderTargetHandle> _outputs;
    private readonly HashSet<RenderTargetHandle> _ownedTargets;
    private bool _disposed;
    private readonly Action<RenderTargetHandle> _releaseOwnedTarget;
    internal RenderGraphExecutionResult(
        Guid graphId,
        IRenderTargetManager renderTargets,
        IReadOnlyDictionary<RenderGraphResourceHandle, RenderTargetHandle> outputs,
        IEnumerable<RenderTargetHandle> ownedTargets,
        Action<RenderTargetHandle> releaseOwnedTarget)
    {
        _graphId = graphId;
        _renderTargets = renderTargets;
        _outputs = outputs.ToDictionary(
            pair => pair.Key,
            pair => pair.Value);
        _ownedTargets = new HashSet<RenderTargetHandle>(ownedTargets);
        _releaseOwnedTarget = releaseOwnedTarget;

    }

    public IReadOnlyCollection<RenderGraphResourceHandle> OutputResources =>
        _outputs.Keys;

    /// <summary>
    /// Gets the physical target associated with a graph output.
    /// The invalid handle represents the window backbuffer.
    /// </summary>
    public RenderTargetHandle GetRenderTarget(RenderGraphResourceHandle resource)
    {
        ThrowIfDisposed();
        ValidateGraph(resource);

        if (!_outputs.TryGetValue(resource, out var target))
        {
            throw new ArgumentException(
                "The resource is not an output of this render graph.",
                nameof(resource));
        }

        return target;
    }

    /// <summary>
    /// Gets the sampleable color texture of a non-backbuffer graph output.
    /// </summary>
    public TextureHandle GetTexture(RenderGraphResourceHandle resource)
    {
        var target = GetRenderTarget(resource);
        if (!target.IsValid)
        {
            throw new InvalidOperationException(
                "The backbuffer does not expose a sampleable color texture.");
        }

        return _renderTargets.GetColorTexture(target);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var errors = new List<Exception>();
        foreach (var target in _ownedTargets.ToArray())
        {
            try
            {
                _releaseOwnedTarget(target);
                _ownedTargets.Remove(target);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more transient render-graph output resources could not be released.",
                errors);
        }

        _disposed = true;
        _outputs.Clear();
    }

    private void ValidateGraph(RenderGraphResourceHandle resource)
    {
        if (!resource.IsValid || resource.GraphId != _graphId)
        {
            throw new ArgumentException(
                "The resource must belong to the graph that produced this result.",
                nameof(resource));
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
