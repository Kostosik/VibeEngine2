using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering.RenderGraph;

public sealed class RenderGraphCompatibilityTests
{
    [Fact]
    public void ExecuteFullscreen_ResolvesInputsWithDifferentFormatsAndColorSpaces()
    {
        var targets = new TestRenderTargetManager();
        var fullscreenExecutor = new TestFullscreenPassExecutor();

        var sceneDescription = new RenderTargetDescription(
            128,
            72,
            TextureFormat.Rgba8,
            hasDepthAttachment: false)
        {
            ColorSpace = TextureColorSpace.Srgb
        };

        var normalDescription = new RenderTargetDescription(
            128,
            72,
            TextureFormat.Rgba16Float,
            hasDepthAttachment: false);

        var lightDescription = new RenderTargetDescription(
            128,
            72,
            TextureFormat.Rgba16Float,
            hasDepthAttachment: true);

        var emissionDescription = new RenderTargetDescription(
            128,
            72,
            TextureFormat.Rgba8,
            hasDepthAttachment: false);

        var backbufferDescription = new RenderTargetDescription(
            128,
            72,
            TextureFormat.Rgba8,
            hasDepthAttachment: false)
        {
            ColorSpace = TextureColorSpace.Srgb
        };

        var sceneTarget = targets.Create(sceneDescription);
        var normalTarget = targets.Create(normalDescription);
        var lightTarget = targets.Create(lightDescription);
        var emissionTarget = targets.Create(emissionDescription);

        var builder = new RenderGraphBuilder();

        var sceneColor = builder.ImportRenderTarget(
            "SceneColor",
            sceneTarget,
            sceneDescription);

        var normalBuffer = builder.ImportRenderTarget(
            "NormalBuffer",
            normalTarget,
            normalDescription);

        var lightBuffer = builder.ImportRenderTarget(
            "LightBuffer",
            lightTarget,
            lightDescription);

        var emissionBuffer = builder.ImportRenderTarget(
            "EmissionBuffer",
            emissionTarget,
            emissionDescription);

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            backbufferDescription);

        var normalSampler = new TextureSamplerDescription(
            TextureFilter.Linear,
            TextureFilter.Linear,
            TextureWrapMode.Repeat,
            TextureWrapMode.MirroredRepeat);

        var fullscreenPass = new FullscreenPassDescription(
            new ShaderHandle(1),
            inputs:
            [
                new FullscreenPassTextureInput("uSceneColor", sceneColor),
                new FullscreenPassTextureInput("uNormalMap", normalBuffer, normalSampler),
                new FullscreenPassTextureInput("uLightBuffer", lightBuffer),
                new FullscreenPassTextureInput("uEmission", emissionBuffer)
            ]);

        builder.AddPass(
            "LightingComposite",
            pass => pass
                .Read(sceneColor)
                .Read(normalBuffer)
                .Read(lightBuffer)
                .Read(emissionBuffer)
                .WriteColor(backbuffer)
                .SetExecute(context => context.ExecuteFullscreen(fullscreenPass)));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        using var executor = new RenderGraphExecutor(
            targets,
            fullscreenExecutor);

        using var result = executor.Execute(plan);

        Assert.Collection(
            fullscreenExecutor.CapturedInputs,
            input =>
            {
                Assert.Equal("uSceneColor", input.Name);
                Assert.Equal(targets.GetColorTexture(sceneTarget), input.Texture);
            },
            input =>
            {
                Assert.Equal("uNormalMap", input.Name);
                Assert.Equal(targets.GetColorTexture(normalTarget), input.Texture);
                Assert.Equal(normalSampler, input.Sampler);
            },
            input =>
            {
                Assert.Equal("uLightBuffer", input.Name);
                Assert.Equal(targets.GetColorTexture(lightTarget), input.Texture);
            },
            input =>
            {
                Assert.Equal("uEmission", input.Name);
                Assert.Equal(targets.GetColorTexture(emissionTarget), input.Texture);
            });

        Assert.Equal(sceneDescription, targets.GetDescription(sceneTarget));
        Assert.Equal(normalDescription, targets.GetDescription(normalTarget));
        Assert.Equal(lightDescription, targets.GetDescription(lightTarget));
        Assert.Equal(emissionDescription, targets.GetDescription(emissionTarget));
        Assert.Equal(backbufferDescription, fullscreenExecutor.OutputDescription);
    }

    [Fact]
    public void Execute_PreservesFormatColorSpaceAndDepthAttachmentForTransientOutputs()
    {
        var targets = new TestRenderTargetManager();

        var floatNoDepthDescription = new RenderGraphResourceDescriptor(
            160,
            90,
            TextureFormat.Rgba16Float)
        {
            ColorSpace = TextureColorSpace.Linear,
            HasDepthAttachment = false
        };

        var srgbNoDepthDescription = new RenderGraphResourceDescriptor(
            160,
            90,
            TextureFormat.Rgba8)
        {
            ColorSpace = TextureColorSpace.Srgb,
            HasDepthAttachment = false
        };

        var builder = new RenderGraphBuilder();

        var floatTarget = builder.CreateTexture(
            "LightBuffer",
            floatNoDepthDescription);

        var srgbTarget = builder.CreateTexture(
            "SrgbMask",
            srgbNoDepthDescription);

        builder.AddPass(
            "ProduceLightBuffer",
            pass => pass.WriteColor(floatTarget));

        builder.AddPass(
            "ProduceSrgbMask",
            pass => pass.WriteColor(srgbTarget));

        builder.SetOutput(floatTarget);
        builder.SetOutput(srgbTarget);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        using var executor = new RenderGraphExecutor(
            targets,
            new TestRenderPassExecutor());

        using var result = executor.Execute(plan);

        var actualFloatDescription = targets.GetDescription(
            result.GetRenderTarget(floatTarget));

        var actualSrgbDescription = targets.GetDescription(
            result.GetRenderTarget(srgbTarget));

        Assert.Equal(
            floatNoDepthDescription.ToRenderTargetDescription(),
            actualFloatDescription);

        Assert.Equal(
            srgbNoDepthDescription.ToRenderTargetDescription(),
            actualSrgbDescription);
    }

    private sealed class TestRenderTargetManager : IRenderTargetManager
    {
        private readonly Dictionary<RenderTargetHandle, Entry> _targets = [];
        private uint _nextTarget = 1;
        private uint _nextTexture = 100;

        public RenderTargetHandle Create(RenderTargetDescription description)
        {
            description.Validate();

            var target = new RenderTargetHandle(_nextTarget++);
            var texture = new TextureHandle(_nextTexture++);

            _targets.Add(target, new Entry(description, texture));
            return target;
        }

        public bool Exists(RenderTargetHandle target) =>
            target.IsValid && _targets.ContainsKey(target);

        public RenderTargetDescription GetDescription(RenderTargetHandle target) =>
            GetEntry(target).Description;

        public TextureHandle GetColorTexture(RenderTargetHandle target) =>
            GetEntry(target).Texture;

        public void Destroy(RenderTargetHandle target)
        {
            _targets.Remove(target);
        }

        public void Resize(RenderTargetHandle target, int width, int height)
        {
            var entry = GetEntry(target);
            var resizedDescription = entry.Description with
            {
                Width = width,
                Height = height
            };

            resizedDescription.Validate();
            _targets[target] = entry with { Description = resizedDescription };
        }

        private Entry GetEntry(RenderTargetHandle target)
        {
            if (!_targets.TryGetValue(target, out var entry))
            {
                throw new KeyNotFoundException(
                    $"Render target '{target.Value}' was not found.");
            }

            return entry;
        }

        private sealed record Entry(
            RenderTargetDescription Description,
            TextureHandle Texture);
    }

    private sealed class TestRenderPassExecutor : IRenderPassExecutor
    {
        public void Begin(RenderPassContext context)
        {
        }

        public void Execute(IRenderCommand command)
        {
        }

        public void End()
        {
        }
    }

    private sealed class TestFullscreenPassExecutor :
        IRenderPassExecutor,
        IFullscreenPassExecutor
    {
        public IReadOnlyList<FullscreenPassTextureBinding> CapturedInputs { get; private set; } = [];

        public RenderTargetDescription OutputDescription { get; private set; }

        public void Begin(RenderPassContext context)
        {
        }

        public void Execute(IRenderCommand command)
        {
        }

        public void End()
        {
        }

        public void ExecuteFullscreen(
            FullscreenPassDescription pass,
            TextureHandle source,
            RenderTargetDescription outputDescription)
        {
            throw new NotSupportedException();
        }

        public void ExecuteFullscreen(
            FullscreenPassDescription pass,
            IReadOnlyList<FullscreenPassTextureBinding> inputs,
            RenderTargetDescription outputDescription)
        {
            CapturedInputs = inputs.ToArray();
            OutputDescription = outputDescription;
        }
    }
}
