using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Converts the legacy RenderPipeline into a compiled render graph.
/// This class is independent of the graphics backend.
/// </summary>
public sealed class RenderPipelineGraphBuilder
{
    private readonly IRenderTargetManager _renderTargetManager;
    private readonly IPostProcessRenderPassExecutor _postProcessExecutor;
    private readonly IFramePresentationExecutor?
    _presentationExecutor;
    private readonly IRenderGraphExtensionRegistry _extensions;

    public IRenderGraphExtensionRegistry Extensions =>
        _extensions;

    public long LastBuildExtensionRevision { get; private set; }

    public RenderPipelineGraphBuilder(
        IRenderTargetManager renderTargetManager,
        IPostProcessRenderPassExecutor postProcessExecutor,
        IRenderGraphExtensionRegistry? extensions = null,
        IFramePresentationExecutor? presentationExecutor = null)
    {
        ArgumentNullException.ThrowIfNull(renderTargetManager);
        ArgumentNullException.ThrowIfNull(postProcessExecutor);

        _renderTargetManager = renderTargetManager;
        _postProcessExecutor = postProcessExecutor;
        _presentationExecutor = presentationExecutor;
        _extensions = extensions ?? new RenderGraphExtensionRegistry();
    }

    public RenderGraphExecutionPlan Build(
        RenderPipeline pipeline,
        int logicalWidth,
        int logicalHeight)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalHeight);

        Validate(pipeline);

        var extensionSnapshot = _extensions.CaptureSnapshot();

        ValidateExtensionTargets(
            pipeline,
            extensionSnapshot.Extensions);

        var extensionsByPass = extensionSnapshot.Extensions
            .GroupBy(
                extension => extension.AfterPipelinePassName,
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal);

        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(
                logicalWidth,
                logicalHeight));

        RenderGraphResourceHandle backbuffer;

        var presentationExecutor = _presentationExecutor;

        if (presentationExecutor is null)
        {
            backbuffer = builder.ImportBackbuffer(
                "Backbuffer",
                new RenderTargetDescription(
                    logicalWidth,
                    logicalHeight));
        }
        else
        {
            var target = presentationExecutor.PresentationTarget;

            if (!target.IsValid ||
                !_renderTargetManager.Exists(target))
            {
                throw new InvalidOperationException(
                    "Presentation render target is not available.");
            }

            var description = _renderTargetManager.GetDescription(target);

            backbuffer = builder.ImportRenderTarget(
                "Backbuffer",
                target,
                description);
        }

        var pipelineTargets =
            new Dictionary<RenderTargetHandle, RenderGraphResourceHandle>();

        RenderGraphResourceHandle ResolveTarget(RenderPass renderPass)
        {
            if (!renderPass.Target.IsValid)
            {
                return renderPass.IsPostProcessSource
                    ? sceneColor
                    : backbuffer;
            }

            if (pipelineTargets.TryGetValue(
                    renderPass.Target,
                    out var existing))
            {
                return existing;
            }

            if (!_renderTargetManager.Exists(renderPass.Target))
            {
                throw new InvalidOperationException(
                    $"Render pass '{renderPass.Name}' references " +
                    $"unknown render target '{renderPass.Target.Value}'.");
            }

            var description = _renderTargetManager.GetDescription(
                renderPass.Target);

            var imported = builder.ImportRenderTarget(
                $"PipelineTarget_{pipelineTargets.Count}",
                renderPass.Target,
                description);

            pipelineTargets.Add(
                renderPass.Target,
                imported);

            return imported;
        }

        RenderGraphPassHandle? previousPass = null;

        for (var i = 0; i < pipeline.Passes.Count; i++)
        {
            var renderPass = pipeline.Passes[i];
            var output = ResolveTarget(renderPass);

            var passName = string.IsNullOrWhiteSpace(renderPass.Name)
                ? $"PipelinePass_{i}"
                : $"PipelinePass_{i}_{renderPass.Name}";

            var currentPass = builder.AddPass(
                passName,
                pass =>
                {
                    pass.WriteColor(
                        output,
                        renderPass.ClearColor
                            ? RenderGraphLoadAction.Clear
                            : RenderGraphLoadAction.Load);

                    pass.SetRenderState(renderPass.State);
                    pass.SetLayerRange(renderPass.Layers);
                    pass.SetContextData(renderPass.ContextData);
                    pass.SetClearDepth(renderPass.ClearDepth);
                    pass.SetSideEffect();

                    if (renderPass.IsPostProcessSource)
                    {
                        pass.SetPostProcessSource();
                    }

                    if (previousPass.HasValue)
                    {
                        pass.DependsOn(previousPass.Value);
                    }
                });

            var stageOutput = output;
            var stageLastPass = currentPass;

            // Подключаем расширения после текущего Pipeline Pass.
            if (extensionsByPass.TryGetValue(
                    renderPass.Name,
                    out var passExtensions))
            {
                foreach (var extension in passExtensions)
                {
                    var extensionContext = new RenderGraphExtensionContext(
                        logicalWidth,
                        logicalHeight,
                        i,
                        renderPass,
                        stageOutput,
                        sceneColor,
                        backbuffer,
                        stageLastPass);

                    var passCountBefore = builder.PassCount;

                    var result = extension.Build(
                        builder,
                        in extensionContext);

                    ValidateExtensionResult(
                        extension,
                        builder,
                        extensionContext,
                        result,
                        passCountBefore,
                        builder.PassCount);

                    if (result.LastPass.HasValue)
                    {
                        stageLastPass = result.LastPass.Value;
                    }

                    if (result.Output.HasValue)
                    {
                        stageOutput = result.Output.Value;
                    }
                }
            }

            previousPass = stageLastPass;

            if (!renderPass.IsPostProcessSource)
            {
                continue;
            }

            // Post Processing использует итоговый результат расширений.
            var postProcessInput = stageOutput;

            var postProcessPass = builder.AddPass(
                $"PipelinePostProcessing_{i}",
                pass =>
                {
                    pass.Read(postProcessInput);

                    pass.WriteColor(
                        backbuffer,
                        RenderGraphLoadAction.DontCare);

                    pass.SetRenderState(RenderState.Default2D);
                    pass.SetExecuteQueuedCommands(false);
                    pass.SetSideEffect();

                    pass.SetExecute(_ =>
                    {
                        _postProcessExecutor.ExecutePostProcessing(
                            pipeline.PostProcessing);
                    });

                    pass.DependsOn(stageLastPass);
                });

            previousPass = postProcessPass;
        }

        if (presentationExecutor is not null)
        {
            var lastPipelinePass = previousPass;

            builder.AddPass(
                "FinalPresent",
                pass =>
                {
                    pass.Read(backbuffer);
                    pass.SetSideEffect();

                    if (lastPipelinePass.HasValue)
                    {
                        pass.DependsOn(lastPipelinePass.Value);
                    }

                    pass.SetExecute(context =>
                    {
                        var source = context.GetTexture(backbuffer);

                        presentationExecutor.ExecutePresent(source);
                    });
                });
        }

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        LastBuildExtensionRevision = extensionSnapshot.Revision;

        return plan;
    }

    public static void Validate(RenderPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        var sourcePassCount = pipeline.Passes.Count(
            pass => pass.IsPostProcessSource);

        var postProcessingEnabled =
            pipeline.PostProcessing.HasEnabledEffects;

        if (sourcePassCount > 1 ||
            (postProcessingEnabled && sourcePassCount != 1))
        {
            throw new InvalidOperationException(
                "An enabled post-process chain requires exactly one " +
                "render pass marked as its source.");
        }
    }

    private static void ValidateExtensionTargets(
    RenderPipeline pipeline,
    IReadOnlyList<IRenderGraphExtension> extensions)
    {
        foreach (var extension in extensions)
        {
            var matches = pipeline.Passes.Count(
                pass => string.Equals(
                    pass.Name,
                    extension.AfterPipelinePassName,
                    StringComparison.Ordinal));

            if (matches == 0)
            {
                throw new InvalidOperationException(
                    $"Extension '{extension.Name}' references " +
                    $"unknown pipeline pass '{extension.AfterPipelinePassName}'.");
            }

            if (matches > 1)
            {
                throw new InvalidOperationException(
                    $"Extension '{extension.Name}' has an ambiguous " +
                    $"pipeline insertion point '{extension.AfterPipelinePassName}'.");
            }
        }
    }

    private static void ValidateExtensionResult(
        IRenderGraphExtension extension,
        RenderGraphBuilder builder,
        RenderGraphExtensionContext context,
        RenderGraphExtensionResult result,
        int passCountBefore,
        int passCountAfter)
    {
        var addedPasses = passCountAfter - passCountBefore;

        if (addedPasses < 0)
        {
            throw new InvalidOperationException(
                $"Extension '{extension.Name}' modified the graph unexpectedly.");
        }

        if (addedPasses == 0)
        {
            if (result.LastPass.HasValue || result.Output.HasValue)
            {
                throw new InvalidOperationException(
                    $"Extension '{extension.Name}' returned graph handles " +
                    "without adding any passes.");
            }

            return;
        }

        if (!result.LastPass.HasValue)
        {
            throw new InvalidOperationException(
                $"Extension '{extension.Name}' added passes but did not return LastPass.");
        }

        var lastPass = result.LastPass.Value;

        if (!lastPass.IsValid ||
            lastPass.GraphId != context.PreviousPass.GraphId ||
            lastPass.Index != passCountAfter - 1)
        {
            throw new InvalidOperationException(
                $"Extension '{extension.Name}' returned an invalid LastPass.");
        }

        if (result.Output.HasValue)
        {
            var output = result.Output.Value;

            if (!output.IsValid ||
                output.GraphId != context.PipelineOutput.GraphId ||
                output.Index >= builder.ResourceCount)
            {
                throw new InvalidOperationException(
                    $"Extension '{extension.Name}' returned an invalid Output.");
            }
        }
    }
}