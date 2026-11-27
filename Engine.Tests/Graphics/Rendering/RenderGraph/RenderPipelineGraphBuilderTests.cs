using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Rendering;

namespace Engine.Tests.Graphics.Rendering.RenderGraph;

public sealed class RenderPipelineGraphBuilderTests
{
    [Fact]
    public void Build_MapsDepthClearIndependentlyFromColorClear()
    {
        var targets = new TestRenderTargetManager();

        var presentation = new TestFrameExecutor(
            targets,
            new RenderTargetDescription(
                1280,
                720,
                TextureFormat.Rgba8,
                hasDepthAttachment: true));

        var pipeline = new RenderPipeline();

        pipeline.AddPass(
            new RenderPass(
                "DepthOnly",
                RenderTargetHandle.Invalid,
                RenderState.Default2D,
                false,
                RenderPassLayerRange.All)
            {
                ClearDepth = true
            });

        var builder = new RenderPipelineGraphBuilder(
            targets,
            presentation,
            presentationExecutor: presentation);

        var plan = builder.Build(pipeline, 1280, 720);

        var compiledPass = Assert.Single(
            plan.Passes,
            pass => pass.Name == "PipelinePass_0_DepthOnly");

        Assert.True(compiledPass.ClearDepth);

        var colorWrite = Assert.Single(
            compiledPass.ResourceAccesses,
            access => access.Kind == RenderGraphResourceAccessKind.WriteColor);

        Assert.Equal(
            RenderGraphLoadAction.Load,
            colorWrite.LoadAction);
    }

    [Fact]
    public void Build_OrdersDefaultPipelineStagesAndPresentation()
    {
        var targets = new TestRenderTargetManager();
        var presentation = new TestFrameExecutor(
            targets,
            new RenderTargetDescription(1280, 720, TextureFormat.Rgba8, hasDepthAttachment: false));

        var pipeline = CreateDefaultPipeline();
        var builder = new RenderPipelineGraphBuilder(
            targets,
            presentation,
            presentationExecutor: presentation);

        var plan = builder.Build(pipeline, 1280, 720);

        Assert.Equal(
            new[]
            {
                "PipelinePass_0_World",
                "PipelinePostProcessing_0",
                "PipelinePass_1_UI",
                "PipelinePass_2_Debug",
                "FinalPresent"
            },
            plan.Passes.Select(pass => pass.Name));

        var world = Assert.Single(
            plan.Passes,
            pass => pass.Name == "PipelinePass_0_World");

        var postProcessing = Assert.Single(
            plan.Passes,
            pass => pass.Name == "PipelinePostProcessing_0");

        var ui = Assert.Single(
            plan.Passes,
            pass => pass.Name == "PipelinePass_1_UI");

        var debug = Assert.Single(
            plan.Passes,
            pass => pass.Name == "PipelinePass_2_Debug");

        var present = Assert.Single(
            plan.Passes,
            pass => pass.Name == "FinalPresent");

        Assert.True(world.IsPostProcessSource);

        var worldOutput = Assert.Single(
            world.ResourceAccesses,
            access => access.Kind == RenderGraphResourceAccessKind.WriteColor);

        var postProcessInput = Assert.Single(
            postProcessing.ResourceAccesses,
            access => access.Kind == RenderGraphResourceAccessKind.Read);

        Assert.Equal(worldOutput.Resource, postProcessInput.Resource);
        Assert.Contains(world.Handle, postProcessing.Dependencies);
        Assert.Contains(postProcessing.Handle, ui.Dependencies);
        Assert.Contains(ui.Handle, debug.Dependencies);
        Assert.Contains(debug.Handle, present.Dependencies);

        Assert.Single(plan.Outputs);

        using var graphExecutor = new RenderGraphExecutor(targets, presentation);
        using var result = graphExecutor.Execute(plan);

        Assert.Equal(
            new[]
            {
                "Begin:World",
                "End",
                "Begin:PostProcessing",
                "PostProcessing",
                "End",
                "Begin:UI",
                "End",
                "Begin:Debug",
                "End",
                "Present"
            },
            presentation.Events);
    }

    [Fact]
    public void Build_OrdersAndChainsMultipleExtensionsAfterWorld()
    {
        var targets = new TestRenderTargetManager();
        var presentation = new TestFrameExecutor(
            targets,
            new RenderTargetDescription(1280, 720, TextureFormat.Rgba8, hasDepthAttachment: false));

        var extensions = new RenderGraphExtensionRegistry();
        var secondExtension = new RecordingExtension(
            name: "SecondWorldExtension",
            order: 20);
        var firstExtension = new RecordingExtension(
            name: "FirstWorldExtension",
            order: 10);

        // Register in reverse order to ensure execution follows Order,
        // not registration order.
        extensions.Register(secondExtension);
        extensions.Register(firstExtension);

        var pipeline = CreateDefaultPipeline();
        var builder = new RenderPipelineGraphBuilder(
            targets,
            presentation,
            extensions,
            presentation);

        var plan = builder.Build(pipeline, 1280, 720);

        Assert.Equal(
            new[]
            {
                "PipelinePass_0_World",
                "FirstWorldExtensionPass",
                "SecondWorldExtensionPass",
                "PipelinePostProcessing_0",
                "PipelinePass_1_UI",
                "PipelinePass_2_Debug",
                "FinalPresent"
            },
            plan.Passes.Select(pass => pass.Name));

        Assert.Equal("World", firstExtension.LastContext.PipelinePass.Name);
        Assert.Equal("World", secondExtension.LastContext.PipelinePass.Name);

        Assert.Equal(
            firstExtension.Output,
            secondExtension.LastContext.PipelineOutput);

        Assert.Equal(
            firstExtension.LastPass,
            secondExtension.LastContext.PreviousPass);

        var firstPass = Assert.Single(
            plan.Passes,
            pass => pass.Name == "FirstWorldExtensionPass");

        var secondPass = Assert.Single(
            plan.Passes,
            pass => pass.Name == "SecondWorldExtensionPass");

        var postProcessing = Assert.Single(
            plan.Passes,
            pass => pass.Name == "PipelinePostProcessing_0");

        Assert.True(firstPass.IsPostProcessSource);
        Assert.True(secondPass.IsPostProcessSource);
        Assert.Contains(firstPass.Handle, secondPass.Dependencies);

        var postProcessInput = Assert.Single(
            postProcessing.ResourceAccesses,
            access => access.Kind == RenderGraphResourceAccessKind.Read);

        Assert.Equal(secondExtension.Output, postProcessInput.Resource);
        Assert.Contains(secondPass.Handle, postProcessing.Dependencies);

        using var graphExecutor = new RenderGraphExecutor(targets, presentation);
        using var result = graphExecutor.Execute(plan);

        Assert.Equal(
            new[]
            {
                "Begin:World",
                "End",
                "Begin:FirstWorldExtension",
                "End",
                "Begin:SecondWorldExtension",
                "End",
                "Begin:PostProcessing",
                "PostProcessing",
                "End",
                "Begin:UI",
                "End",
                "Begin:Debug",
                "End",
                "Present"
            },
            presentation.Events);
    }

    [Fact]
    public void Build_RejectsExtensionThatReferencesUnknownPipelinePass()
    {
        var targets = new TestRenderTargetManager();
        var presentation = new TestFrameExecutor(
            targets,
            new RenderTargetDescription(1280, 720, TextureFormat.Rgba8, hasDepthAttachment: false));

        var extensions = new RenderGraphExtensionRegistry();
        extensions.Register(
            new RecordingExtension(
                name: "UnknownPassExtension",
                order: 0,
                afterPipelinePassName: "MissingPass"));

        var builder = new RenderPipelineGraphBuilder(
            targets,
            presentation,
            extensions,
            presentation);

        Assert.Throws<InvalidOperationException>(
            () => builder.Build(CreateDefaultPipeline(), 1280, 720));
    }

    [Fact]
    public void Build_RebuildsGraphAfterResizeAndUsesUpdatedTargetDescriptions()
    {
        var targets = new TestRenderTargetManager();
        var initialPresentationDescription = new RenderTargetDescription(
            1280,
            720,
            TextureFormat.Rgba8,
            hasDepthAttachment: false)
        {
            ColorSpace = TextureColorSpace.Srgb
        };

        var presentation = new TestFrameExecutor(
            targets,
            initialPresentationDescription);

        var extensions = new RenderGraphExtensionRegistry();
        var resizeExtension = new RecordingExtension(
            name: "ResizeExtension",
            order: 0);
        extensions.Register(resizeExtension);

        var pipeline = CreateDefaultPipeline();
        var builder = new RenderPipelineGraphBuilder(
            targets,
            presentation,
            extensions,
            presentation);

        var initialPlan = builder.Build(pipeline, 1280, 720);

        var initialSceneColor = Assert.Single(
            initialPlan.Resources,
            resource => resource.Name == "SceneColor");
        var initialBackbuffer = Assert.Single(
            initialPlan.Resources,
            resource => resource.Name == "Backbuffer");
        var initialExtensionOutput = Assert.Single(
            initialPlan.Resources,
            resource => resource.Name == "ResizeExtensionOutput");

        Assert.Equal(1280, initialSceneColor.Descriptor.Width);
        Assert.Equal(720, initialSceneColor.Descriptor.Height);
        Assert.Equal(1280, initialBackbuffer.Descriptor.Width);
        Assert.Equal(720, initialBackbuffer.Descriptor.Height);
        Assert.Equal(initialPresentationDescription,
            initialBackbuffer.Descriptor.ToRenderTargetDescription());
        Assert.Equal(1280, initialExtensionOutput.Descriptor.Width);
        Assert.Equal(720, initialExtensionOutput.Descriptor.Height);
        Assert.Equal(1280, resizeExtension.LastContext.Width);
        Assert.Equal(720, resizeExtension.LastContext.Height);

        using var graphExecutor = new RenderGraphExecutor(
            targets,
            presentation);

        using (graphExecutor.Execute(initialPlan))
        {
        }

        presentation.Events.Clear();

        var resizedPresentationDescription = initialPresentationDescription with
        {
            Width = 960,
            Height = 540
        };

        targets.Resize(
            presentation.PresentationTarget,
            resizedPresentationDescription.Width,
            resizedPresentationDescription.Height);

        var resizedPlan = builder.Build(pipeline, 960, 540);

        Assert.NotEqual(initialPlan.GraphId, resizedPlan.GraphId);

        var resizedSceneColor = Assert.Single(
            resizedPlan.Resources,
            resource => resource.Name == "SceneColor");
        var resizedBackbuffer = Assert.Single(
            resizedPlan.Resources,
            resource => resource.Name == "Backbuffer");
        var resizedExtensionOutput = Assert.Single(
            resizedPlan.Resources,
            resource => resource.Name == "ResizeExtensionOutput");

        Assert.Equal(960, resizedSceneColor.Descriptor.Width);
        Assert.Equal(540, resizedSceneColor.Descriptor.Height);
        Assert.Equal(960, resizedBackbuffer.Descriptor.Width);
        Assert.Equal(540, resizedBackbuffer.Descriptor.Height);
        Assert.Equal(resizedPresentationDescription,
            resizedBackbuffer.Descriptor.ToRenderTargetDescription());
        Assert.Equal(960, resizedExtensionOutput.Descriptor.Width);
        Assert.Equal(540, resizedExtensionOutput.Descriptor.Height);
        Assert.False(resizedExtensionOutput.Descriptor.HasDepthAttachment);
        Assert.Equal(960, resizeExtension.LastContext.Width);
        Assert.Equal(540, resizeExtension.LastContext.Height);

        using (graphExecutor.Execute(resizedPlan))
        {
        }

        Assert.Equal(
            new[]
            {
                "Begin:World",
                "End",
                "Begin:ResizeExtension",
                "End",
                "Begin:PostProcessing",
                "PostProcessing",
                "End",
                "Begin:UI",
                "End",
                "Begin:Debug",
                "End",
                "Present"
            },
            presentation.Events);
    }

    private static RenderPipeline CreateDefaultPipeline()
    {
        var pipeline = new RenderPipeline();
        pipeline.AddPass(
            RenderPass2D.World with
            {
                ContextData = new TestPassContextData("World")
            });

        pipeline.AddPass(
            RenderPass2D.Ui with
            {
                ContextData = new TestPassContextData("UI")
            });

        pipeline.AddPass(
            RenderPass2D.Debug with
            {
                ContextData = new TestPassContextData("Debug")
            });
        return pipeline;
    }

    private sealed class RecordingExtension : IRenderGraphExtension
    {
        public RecordingExtension(
            string name,
            int order,
            string afterPipelinePassName = "World")
        {
            Name = name;
            Order = order;
            AfterPipelinePassName = afterPipelinePassName;
        }

        public string Name { get; }

        public string AfterPipelinePassName { get; }

        public int Order { get; }

        public RenderGraphExtensionContext LastContext { get; private set; }

        public RenderGraphResourceHandle Output { get; private set; }

        public RenderGraphPassHandle LastPass { get; private set; }

        public RenderGraphExtensionResult Build(
            RenderGraphBuilder builder,
            in RenderGraphExtensionContext context)
        {
            LastContext = context;

            // Copy values out of the in-parameter before using them in the
            // pass-builder lambda.
            var input = context.PipelineOutput;
            var previousPass = context.PreviousPass;

            Output = builder.CreateTexture(
                $"{Name}Output",
                new RenderGraphResourceDescriptor(
                    context.Width,
                    context.Height)
                {
                    HasDepthAttachment = false
                });

            LastPass = builder.AddPass(
                $"{Name}Pass",
                pass => pass
                    .Read(input)
                    .WriteColor(Output)
                    .DependsOn(previousPass)
                    .SetContextData(new TestPassContextData(Name))
                    .SetPostProcessSource());

            return new RenderGraphExtensionResult(
                LastPass,
                Output);
        }
    }

    private sealed record TestPassContextData(string Name) : IRenderPassContextData;

    private sealed class TestFrameExecutor :
        IRenderPassExecutor,
        IPostProcessRenderPassExecutor,
        IFramePresentationExecutor
    {
        public TestFrameExecutor(
            TestRenderTargetManager targets,
            RenderTargetDescription presentationDescription)
        {
            PresentationTarget = targets.Create(presentationDescription);
        }

        public RenderTargetHandle PresentationTarget { get; }

        public List<string> Events { get; } = new();

        public void Begin(RenderPassContext context)
        {
            var passName = context.ContextData is TestPassContextData data
                ? data.Name
                : "PostProcessing";

            Events.Add($"Begin:{passName}");
        }

        public void Execute(IRenderCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
        }

        public void End()
        {
            Events.Add("End");
        }

        public void ExecutePostProcessing(PostProcessChain chain)
        {
            ArgumentNullException.ThrowIfNull(chain);
            Events.Add("PostProcessing");
        }

        public void ExecutePresent(TextureHandle source)
        {
            if (!source.IsValid)
            {
                throw new ArgumentException(
                    "Presentation source texture is invalid.",
                    nameof(source));
            }

            Events.Add("Present");
        }
    }

    private sealed class TestRenderTargetManager : IRenderTargetManager
    {
        private readonly Dictionary<RenderTargetHandle, TargetEntry> _targets = new();
        private uint _nextTarget = 1;
        private uint _nextTexture = 1;

        public RenderTargetHandle Create(RenderTargetDescription description)
        {
            description.Validate();

            var target = new RenderTargetHandle(_nextTarget++);
            var texture = new TextureHandle(_nextTexture++);

            _targets.Add(
                target,
                new TargetEntry(description, texture));

            return target;
        }

        public bool Exists(RenderTargetHandle target)
        {
            return target.IsValid && _targets.ContainsKey(target);
        }

        public RenderTargetDescription GetDescription(RenderTargetHandle target)
        {
            return GetEntry(target).Description;
        }

        public TextureHandle GetColorTexture(RenderTargetHandle target)
        {
            return GetEntry(target).ColorTexture;
        }

        public void Destroy(RenderTargetHandle target)
        {
            _targets.Remove(target);
        }

        public void Resize(
            RenderTargetHandle target,
            int width,
            int height)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

            var current = GetEntry(target);
            var resizedDescription = new RenderTargetDescription(
                width,
                height,
                current.Description.Format,
                current.Description.HasDepthAttachment)
            {
                ColorSpace = current.Description.ColorSpace
            };

            resizedDescription.Validate();
            _targets[target] = current with { Description = resizedDescription };
        }

        private TargetEntry GetEntry(RenderTargetHandle target)
        {
            if (!_targets.TryGetValue(target, out var entry))
            {
                throw new InvalidOperationException(
                    $"Render target '{target.Value}' does not exist.");
            }

            return entry;
        }

        private readonly record struct TargetEntry(
            RenderTargetDescription Description,
            TextureHandle ColorTexture);
    }
}
