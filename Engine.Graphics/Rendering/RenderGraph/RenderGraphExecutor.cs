using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Executes a compiled render graph using the existing render-pass executor
/// and render-target manager abstractions. It does not depend on OpenGL.
/// </summary>
public sealed class RenderGraphExecutor : IDisposable
{
    private readonly TransientRenderTargetPool _transientPool;
    private bool _disposed;
    private readonly IRenderTargetManager _renderTargets;
    private readonly IRenderPassExecutor _passExecutor;

    public RenderGraphExecutor(
        IRenderTargetManager renderTargets,
        IRenderPassExecutor passExecutor)
    {
        ArgumentNullException.ThrowIfNull(renderTargets);
        ArgumentNullException.ThrowIfNull(passExecutor);

        _renderTargets = renderTargets;
        _passExecutor = passExecutor;
        _transientPool = new TransientRenderTargetPool(_renderTargets);
    }

    /// <summary>
    /// Executes the supplied plan. Transient resources are acquired from the
    /// executor-owned pool and released after their last use. Transient outputs
    /// remain leased to the returned result while this executor is alive.
    /// Disposing the executor destroys all targets owned by its pool, including
    /// outputs still retained by execution results.
    /// </summary>

    public RenderGraphExecutionResult Execute(
        RenderGraphExecutionPlan plan)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Execute(plan, null);
    }

    public RenderGraphExecutionResult Execute(
        RenderGraphExecutionPlan plan,
        RenderQueue? queue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(plan);

        // Queue sorting is performed once for the entire graph.
        queue?.Sort();

        var resources = plan.Resources.ToDictionary(
            resource => resource.Handle);

        var physicalTargets =
            new Dictionary<RenderGraphResourceHandle, RenderTargetHandle>();

        var allocatedTransientTargets =
            new HashSet<RenderTargetHandle>();

        ValidateAndImportExternalResources(
            plan.Resources,
            physicalTargets);

        try
        {
            for (var passIndex = 0;
                 passIndex < plan.Passes.Count;
                 passIndex++)
            {
                AllocateResourcesAtFirstUse(
                    plan.Resources,
                    passIndex,
                    physicalTargets,
                    allocatedTransientTargets);

                ExecutePass(
                    plan.Passes[passIndex],
                    resources,
                    physicalTargets,
                    queue);

                ReleaseResourcesAtLastUse(
                    plan.Resources,
                    passIndex,
                    physicalTargets,
                    allocatedTransientTargets);
            }

            var outputs = ResolveOutputs(
                plan,
                physicalTargets);

            var retainedTargets =
                new HashSet<RenderTargetHandle>();

            foreach (var output in plan.Resources.Where(resource =>
                         resource.IsOutput &&
                         resource.Kind == RenderGraphResourceKind.Transient))
            {
                if (!physicalTargets.TryGetValue(
                        output.Handle,
                        out var target) ||
                    !target.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Transient output '{output.Name}' was not allocated during graph execution.");
                }

                retainedTargets.Add(target);
            }

            DestroyUnretainedTransientTargets(
                allocatedTransientTargets,
                retainedTargets);

            return new RenderGraphExecutionResult(
                plan.GraphId,
                _renderTargets,
                outputs,
                retainedTargets,
                _transientPool.Release);
        }
        catch (Exception executionException)
        {
            var cleanupErrors =
                DestroyAllTransientTargets(allocatedTransientTargets);

            if (cleanupErrors.Count > 0)
            {
                cleanupErrors.Insert(0, executionException);

                throw new AggregateException(
                    "Render-graph execution failed and transient resources could not all be released.",
                    cleanupErrors);
            }

            throw;
        }
    }


    private void ValidateAndImportExternalResources(
        IReadOnlyList<RenderGraphCompiledResource> resources,
        IDictionary<RenderGraphResourceHandle, RenderTargetHandle> physicalTargets)
    {
        foreach (var resource in resources)
        {
            resource.Descriptor.Validate();

            switch (resource.Kind)
            {
                case RenderGraphResourceKind.Transient:
                    break;

                case RenderGraphResourceKind.ImportedRenderTarget:
                {
                    if (!resource.ImportedRenderTarget.IsValid ||
                        !_renderTargets.Exists(resource.ImportedRenderTarget))
                    {
                        throw new InvalidOperationException(
                            $"Imported render target '{resource.Name}' does not exist.");
                    }

                    var actual = _renderTargets.GetDescription(resource.ImportedRenderTarget);
                    var expected = resource.Descriptor.ToRenderTargetDescription();
                    if (actual != expected)
                    {
                        throw new InvalidOperationException(
                            $"Imported render target '{resource.Name}' has description " +
                            $"'{actual}', but the graph expects '{expected}'.");
                    }

                    physicalTargets.Add(resource.Handle, resource.ImportedRenderTarget);
                    break;
                }

                case RenderGraphResourceKind.Backbuffer:
                    physicalTargets.Add(resource.Handle, RenderTargetHandle.Invalid);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Resource '{resource.Name}' has an unsupported resource kind '{resource.Kind}'.");
            }
        }
    }

    private void AllocateResourcesAtFirstUse(
        IReadOnlyList<RenderGraphCompiledResource> resources,
        int passIndex,
        IDictionary<RenderGraphResourceHandle, RenderTargetHandle> physicalTargets,
        ISet<RenderTargetHandle> allocatedTargets)
    {
        foreach (var resource in resources)
        {
            if (resource.Kind != RenderGraphResourceKind.Transient ||
                resource.FirstUseIndex != passIndex)
            {
                continue;
            }

            var target = _transientPool.Acquire(
                resource.Descriptor.ToRenderTargetDescription());

            if (!target.IsValid)
            {
                throw new InvalidOperationException(
                    $"Render-target manager failed to allocate transient resource '{resource.Name}'.");
            }

            physicalTargets.Add(resource.Handle, target);
            allocatedTargets.Add(target);
        }
    }

    private void ExecutePass(
        RenderGraphCompiledPass pass,
        IReadOnlyDictionary<RenderGraphResourceHandle, RenderGraphCompiledResource> resources,
        IReadOnlyDictionary<RenderGraphResourceHandle, RenderTargetHandle> physicalTargets,
        RenderQueue? queue)
    {
        var writeAccesses = pass.ResourceAccesses
            .Where(access => access.Kind == RenderGraphResourceAccessKind.WriteColor)
            .ToArray();

        if (writeAccesses.Length > 1)
        {
            throw new InvalidOperationException(
                $"Pass '{pass.Name}' declares more than one color output.");
        }

        var hasColorOutput = writeAccesses.Length == 1;
        var colorTarget = hasColorOutput
            ? physicalTargets[writeAccesses[0].Resource]
            : RenderTargetHandle.Invalid;

        var clearColor = hasColorOutput &&
                         writeAccesses[0].LoadAction == RenderGraphLoadAction.Clear;

        var context = new RenderGraphContext(
            pass.Name,
            colorTarget,
            pass.ResourceAccesses,
            resources,
            physicalTargets,
            _renderTargets,
            _passExecutor,
            _passExecutor as IFullscreenPassExecutor,
            hasColorOutput);

        if (!hasColorOutput)
        {
            pass.Execute?.Invoke(context);
            return;
        }

        var passContext = new RenderPassContext(
            colorTarget,
            pass.State,
            clearColor)
        {
            IsPostProcessSource = pass.IsPostProcessSource,
            ContextData = pass.ContextData,
            ClearDepth = pass.ClearDepth,
            ClearColorValue = pass.ClearColorValue
        };

        var begun = false;
        try
        {
            _passExecutor.Begin(passContext);
            begun = true;

            if (pass.ExecuteQueuedCommands)
            {
                ExecuteQueuedCommands(
                    queue,
                    pass.Layers,
                    context);
            }

            pass.Execute?.Invoke(context);
        }
        finally
        {
            if (begun)
            {
                _passExecutor.End();
            }
        }
    }

    private static void ExecuteQueuedCommands(
    RenderQueue? queue,
    RenderPassLayerRange layers,
    RenderGraphContext context)
    {
        if (queue is null)
            return;

        foreach (var item in queue.Items)
        {
            if (!layers.Contains(item.Layer))
                continue;

            if (item.HasWorldTextureData)
            {
                context.Execute(
                    queue.GetWorldTextureData(
                        item.WorldTextureDataIndex));
            }
            else
            {
                context.Execute(item.Command);
            }
        }
    }

    private void ReleaseResourcesAtLastUse(
        IReadOnlyList<RenderGraphCompiledResource> resources,
        int passIndex,
        IDictionary<RenderGraphResourceHandle, RenderTargetHandle> physicalTargets,
        ISet<RenderTargetHandle> allocatedTargets)
    {
        foreach (var resource in resources)
        {
            if (resource.Kind != RenderGraphResourceKind.Transient ||
                resource.IsOutput ||
                resource.LastUseIndex != passIndex)
            {
                continue;
            }

            if (!physicalTargets.TryGetValue(resource.Handle, out var target))
            {
                continue;
            }

            _transientPool.Release(target);
            allocatedTargets.Remove(target);
            physicalTargets.Remove(resource.Handle);
        }
    }

    private IReadOnlyDictionary<RenderGraphResourceHandle, RenderTargetHandle> ResolveOutputs(
        RenderGraphExecutionPlan plan,
        IReadOnlyDictionary<RenderGraphResourceHandle, RenderTargetHandle> physicalTargets)
    {
        var result = new Dictionary<RenderGraphResourceHandle, RenderTargetHandle>();
        foreach (var output in plan.Outputs)
        {
            if (!physicalTargets.TryGetValue(output, out var target))
            {
                throw new InvalidOperationException(
                    $"Graph output '{output}' has no resolved physical resource.");
            }

            result.Add(output, target);
        }

        return result;
    }

    private void DestroyUnretainedTransientTargets(
        ISet<RenderTargetHandle> allocatedTargets,
        IReadOnlySet<RenderTargetHandle> retainedTargets)
    {
        var errors = new List<Exception>();
        foreach (var target in allocatedTargets.ToArray())
        {
            if (retainedTargets.Contains(target))
            {
                continue;
            }

            try
            {
                _transientPool.Release(target);
                allocatedTargets.Remove(target);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more transient render-graph resources could not be released.",
                errors);
        }
    }

    private List<Exception> DestroyAllTransientTargets(
        ISet<RenderTargetHandle> allocatedTargets)
    {
        var errors = new List<Exception>();
        foreach (var target in allocatedTargets.ToArray())
        {
            try
            {
                _transientPool.Release(target);
                allocatedTargets.Remove(target);
            }
            catch (Exception exception)
            {   
                errors.Add(exception);
            }
        }

        return errors;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _transientPool.Dispose();
    }

    private sealed class TransientRenderTargetPool : IDisposable
    {
        private const int MaxCachedTargets = 16;
        private const int MaxCachedPerDescription = 2;

        private readonly IRenderTargetManager _renderTargets;

        private readonly Dictionary<
            RenderTargetDescription,
            Stack<RenderTargetHandle>> _available = new();

        private readonly Dictionary<
            RenderTargetHandle,
            RenderTargetDescription> _knownTargets = new();

        private readonly HashSet<RenderTargetHandle> _leasedTargets = new();

        private int _cachedCount;
        private bool _disposed;

        public TransientRenderTargetPool(IRenderTargetManager renderTargets)
        {
            ArgumentNullException.ThrowIfNull(renderTargets);
            _renderTargets = renderTargets;
        }

        public RenderTargetHandle Acquire(RenderTargetDescription description)
        {
            ThrowIfDisposed();
            description.Validate();

            if (_available.TryGetValue(description, out var available))
            {
                while (available.Count > 0)
                {
                    var target = available.Pop();
                    _cachedCount--;

                    // Если ресурс был потерян, забываем о нём.
                    if (!_renderTargets.Exists(target))
                    {
                        _knownTargets.Remove(target);
                        continue;
                    }

                    _leasedTargets.Add(target);
                    return target;
                }
            }

            var createdTarget = _renderTargets.Create(description);

            if (!createdTarget.IsValid)
            {
                throw new InvalidOperationException(
                    "Failed to create a transient render target.");
            }

            _knownTargets.Add(createdTarget, description);
            _leasedTargets.Add(createdTarget);

            return createdTarget;
        }

        public void Release(RenderTargetHandle target)
        {
            if (!target.IsValid || _disposed)
            {
                return;
            }

            if (!_knownTargets.TryGetValue(target, out var description))
            {
                throw new InvalidOperationException(
                    $"Render target '{target.Value}' does not belong to this pool.");
            }

            // Защита от повторного возвращения одного ресурса.
            if (!_leasedTargets.Remove(target))
            {
                return;
            }

            if (!_renderTargets.Exists(target))
            {
                _knownTargets.Remove(target);
                return;
            }

            if (!_available.TryGetValue(description, out var available))
            {
                available = new Stack<RenderTargetHandle>();
                _available.Add(description, available);
            }

            // Ограничиваем количество сохранённых GPU-ресурсов.
            if (_cachedCount >= MaxCachedTargets ||
                available.Count >= MaxCachedPerDescription)
            {
                _renderTargets.Destroy(target);
                _knownTargets.Remove(target);
                return;
            }

            available.Push(target);
            _cachedCount++;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var errors = new List<Exception>();

            // Уничтожаем и свободные, и ещё занятые пулом ресурсы.
            foreach (var target in _knownTargets.Keys.ToArray())
            {
                try
                {
                    if (_renderTargets.Exists(target))
                    {
                        _renderTargets.Destroy(target);
                    }
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            _available.Clear();
            _knownTargets.Clear();
            _leasedTargets.Clear();
            _cachedCount = 0;

            if (errors.Count > 0)
            {
                throw new AggregateException(
                    "Failed to dispose one or more pooled render targets.",
                    errors);
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

}

