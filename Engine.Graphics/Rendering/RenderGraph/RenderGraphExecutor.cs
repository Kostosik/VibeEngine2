using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Executes a compiled render graph using the existing render-pass executor
/// and render-target manager abstractions. It does not depend on OpenGL.
/// </summary>
public sealed class RenderGraphExecutor
{
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
    }

    /// <summary>
    /// Executes the supplied plan. Graph-owned transient resources are created
    /// on first use and released after their last use. Transient outputs are
    /// transferred to the returned result and remain alive until it is disposed.
    /// </summary>
    public RenderGraphExecutionResult Execute(RenderGraphExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var resources = plan.Resources.ToDictionary(resource => resource.Handle);
        var physicalTargets = new Dictionary<RenderGraphResourceHandle, RenderTargetHandle>();
        var allocatedTransientTargets = new HashSet<RenderTargetHandle>();

        ValidateAndImportExternalResources(plan.Resources, physicalTargets);

        try
        {
            for (var passIndex = 0; passIndex < plan.Passes.Count; passIndex++)
            {
                AllocateResourcesAtFirstUse(
                    plan.Resources,
                    passIndex,
                    physicalTargets,
                    allocatedTransientTargets);

                ExecutePass(
                    plan.Passes[passIndex],
                    resources,
                    physicalTargets);

                ReleaseResourcesAtLastUse(
                    plan.Resources,
                    passIndex,
                    physicalTargets,
                    allocatedTransientTargets);
            }

            var outputs = ResolveOutputs(
                plan,
                physicalTargets);

            var retainedTargets = new HashSet<RenderTargetHandle>();
            foreach (var output in plan.Resources.Where(resource =>
                         resource.IsOutput &&
                         resource.Kind == RenderGraphResourceKind.Transient))
            {
                if (!physicalTargets.TryGetValue(output.Handle, out var target) ||
                    !target.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Transient output '{output.Name}' was not allocated during graph execution.");
                }

                retainedTargets.Add(target);
            }

            DestroyUnretainedTransientTargets(allocatedTransientTargets, retainedTargets);

            return new RenderGraphExecutionResult(
                plan.GraphId,
                _renderTargets,
                outputs,
                retainedTargets);
        }
        catch (Exception executionException)
        {
            var cleanupErrors = DestroyAllTransientTargets(allocatedTransientTargets);
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

            var target = _renderTargets.Create(
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
        IReadOnlyDictionary<RenderGraphResourceHandle, RenderTargetHandle> physicalTargets)
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
            hasColorOutput);

        if (!hasColorOutput)
        {
            pass.Execute?.Invoke(context);
            return;
        }

        var passContext = new RenderPassContext(
            colorTarget,
            pass.State,
            clearColor);

        var begun = false;
        try
        {
            _passExecutor.Begin(passContext);
            begun = true;

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

            _renderTargets.Destroy(target);
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
                _renderTargets.Destroy(target);
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
                _renderTargets.Destroy(target);
                allocatedTargets.Remove(target);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        return errors;
    }

}

