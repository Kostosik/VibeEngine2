using Engine.Camera;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;
using Engine.Core.Math;

namespace Engine.Graphics2D.Lighting;

public sealed class LightingRenderGraphExtension2D :
    IRenderGraphExtension
{
    private readonly LightingCore2D _lightingCore;
    private readonly Func<Camera2D> _cameraProvider;
    private readonly ILightingPassRenderer2D _renderer;
    private readonly ILightingCompositeRenderer2D _compositeRenderer;

    public string Name => "Lighting2D";

    public string AfterPipelinePassName => "World";

    public int Order => 0;

    public LightingRenderGraphExtension2D(
        LightingCore2D lightingCore,
        Func<Camera2D> cameraProvider,
        ILightingPassRenderer2D renderer,
        ILightingCompositeRenderer2D compositeRenderer)
    {
        ArgumentNullException.ThrowIfNull(lightingCore);
        ArgumentNullException.ThrowIfNull(cameraProvider);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(compositeRenderer);

        _lightingCore = lightingCore;
        _cameraProvider = cameraProvider;
        _renderer = renderer;
        _compositeRenderer = compositeRenderer;
    }

    public RenderGraphExtensionResult Build(
        RenderGraphBuilder builder,
        in RenderGraphExtensionContext context)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            context.Width);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            context.Height);

        var width = context.Width;
        var height = context.Height;
        var sceneColor = context.SceneColor;
        var previousPass = context.PreviousPass;

        var buffers = LightingBufferSet2D.Create(
            builder,
            width,
            height,
            Name);

        var lightedScene = builder.CreateTexture(
            $"{Name}.LightedScene",
            new RenderGraphResourceDescriptor(
                width,
                height,
                TextureFormat.Rgba16Float)
            {
                ColorSpace = TextureColorSpace.Linear,
                HasDepthAttachment = false
            });

        var frameState = new LightingFrameState();

        var collectPass = builder.AddPass(
            $"{Name}.Collect",
            pass =>
            {
                pass.SetExecuteQueuedCommands(false);
                pass.DependsOn(previousPass);

                pass.SetExecute(_ =>
                {
                    var camera = _cameraProvider()
                        ?? throw new InvalidOperationException(
                            "The 2D lighting camera is not available.");

                    var lightingContext = new LightingContext2D(
                        camera,
                        width,
                        height);

                    frameState.Context = lightingContext;

                    frameState.Result = _lightingCore.Collect(
                        lightingContext);
                });
            });
        var pipelinePassState = context.PipelinePass.State;
        var pipelinePassLayers = context.PipelinePass.Layers;

        var normalPassState = pipelinePassState with
        {
            Blend = new RenderBlendState(RenderBlendMode.Alpha),
            Depth = pipelinePassState.Depth with
            {
                TestEnabled = false,
                WriteEnabled = false
            }
        };

        var normalBufferPass = builder.AddPass(
            $"{Name}.NormalBuffer",
            pass =>
            {
                pass.WriteColor(
                    buffers.NormalBuffer,
                    RenderGraphLoadAction.Clear);

                pass.SetClearColor(
                    new Vector4(0.5f, 0.5f, 1f, 1f));

                pass.SetRenderState(normalPassState);
                pass.SetLayerRange(pipelinePassLayers);

                pass.SetContextData(
                    new NormalBufferPassContext2D());

                pass.SetExecuteQueuedCommands(true);
                pass.SetContextData(new NormalBufferPassContext2D());
                // Сохраняем проход, пока буфер ещё не используется
                // проходами освещения.
                pass.SetSideEffect();

                pass.DependsOn(collectPass);
            });

        var currentPass = normalBufferPass;

        currentPass = AddChannelPass(
            builder,
            "DirectLighting",
            LightingChannel2D.Direct,
            buffers.DirectLighting,
            sceneColor,
            buffers.NormalBuffer,
            currentPass,
            frameState);

        currentPass = AddChannelPass(
            builder,
            "AmbientLighting",
            LightingChannel2D.Ambient,
            buffers.AmbientLighting,
            sceneColor,
            buffers.NormalBuffer,
            currentPass,
            frameState);

        currentPass = AddChannelPass(
            builder,
            "IndirectLighting",
            LightingChannel2D.Indirect,
            buffers.IndirectLighting,
            sceneColor,
            buffers.NormalBuffer,
            currentPass,
            frameState);

        currentPass = AddChannelPass(
            builder,
            "Emission",
            LightingChannel2D.Emission,
            buffers.Emission,
            sceneColor,
            buffers.NormalBuffer,
            currentPass,
            frameState);

        var compositeReadResources = new[]
            {
                sceneColor,
                buffers.DirectLighting,
                buffers.AmbientLighting,
                buffers.IndirectLighting,
                buffers.Emission
            }
            .Distinct()
            .ToArray();

        var compositePass = builder.AddPass(
            $"{Name}.Composite",
            pass =>
            {
                foreach (var resource in compositeReadResources)
                {
                    pass.Read(resource);
                }

                pass.WriteColor(
                    lightedScene,
                    RenderGraphLoadAction.Clear);

                pass.SetExecuteQueuedCommands(false);

                // The composite result replaces the post-process source.
                pass.SetPostProcessSource();

                pass.DependsOn(currentPass);

                pass.SetExecute(graphContext =>
                {
                    var compositeContext =
                        new LightingCompositeContext2D(
                            sceneColor,
                            buffers.DirectLighting,
                            buffers.AmbientLighting,
                            buffers.IndirectLighting,
                            buffers.Emission,
                            lightedScene);

                    _compositeRenderer.Composite(
                        in compositeContext,
                        graphContext);
                });
            });

        return new RenderGraphExtensionResult(
            compositePass,
            lightedScene);
    }

    private RenderGraphPassHandle AddChannelPass(
        RenderGraphBuilder builder,
        string passName,
        LightingChannel2D channel,
        RenderGraphResourceHandle output,
        RenderGraphResourceHandle sceneColor,
        RenderGraphResourceHandle normalBuffer,
        RenderGraphPassHandle previousPass,
        LightingFrameState frameState)
    {
        return builder.AddPass(
            $"{Name}.{passName}",
            pass =>
            {
                pass.Read(sceneColor);

                if (channel == LightingChannel2D.Direct)
                {
                    pass.Read(normalBuffer);
                }

                pass.WriteColor(
                    output,
                    RenderGraphLoadAction.Clear);

                pass.SetExecuteQueuedCommands(false);
                pass.DependsOn(previousPass);

                pass.SetExecute(graphContext =>
                {
                    var result = frameState.Result
                        ?? throw new InvalidOperationException(
                            "Lighting data was not collected before channel execution.");

                    var passContext = new LightingPassContext2D(
                        channel,
                        frameState.Context,
                        result,
                        sceneColor,
                        normalBuffer,
                        output);

                    _renderer.Render(
                        in passContext,
                        graphContext);
                });
            });
    }

    private sealed class LightingFrameState
    {
        public LightingContext2D Context { get; set; }

        public LightingResult2D? Result { get; set; }
    }
}