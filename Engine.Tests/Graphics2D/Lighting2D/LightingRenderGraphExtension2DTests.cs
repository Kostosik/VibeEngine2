using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Lighting;
using Engine.Graphics2D.Rendering;

namespace Engine.Tests.Graphics2D.Lighting2D;

public sealed class LightingRenderGraphExtension2DTests
{
    [Fact]
    public void Build_InsertsCompositeAfterLightingChannels()
    {
        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(640, 480));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                sceneColor,
                RenderGraphLoadAction.Clear));

        var extension = CreateExtension();

        var context = new RenderGraphExtensionContext(
            640,
            480,
            0,
            RenderPass2D.World,
            sceneColor,
            sceneColor,
            backbuffer,
            worldPass);

        var result = extension.Build(
            builder,
            in context);

        Assert.True(result.Output.HasValue);

        builder.SetOutput(result.Output.Value);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        Assert.Equal(
            new[]
            {
                "World",
                "Lighting2D.Collect",
                "Lighting2D.NormalBuffer",
                "Lighting2D.DirectLighting",
                "Lighting2D.AmbientLighting",
                "Lighting2D.IndirectLighting",
                "Lighting2D.Emission",
                "Lighting2D.Composite"
            },
            plan.Passes.Select(pass => pass.Name));

        Assert.Equal(
            plan.Passes[^1].Handle,
            result.LastPass);

        Assert.Contains(
            plan.Resources,
            resource => resource.Handle == result.Output.Value &&
                        resource.Name == "Lighting2D.LightedScene");
    }

    [Fact]
    public void NormalBufferPass_ClearsToNeutralWorldNormal()
    {
        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(640, 480));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                sceneColor,
                RenderGraphLoadAction.Clear));

        var extension = CreateExtension();

        var context = new RenderGraphExtensionContext(
            640,
            480,
            0,
            RenderPass2D.World,
            sceneColor,
            sceneColor,
            backbuffer,
            worldPass);

        var result = extension.Build(
            builder,
            in context);

        builder.SetOutput(result.Output!.Value);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var normalPass = Assert.Single(
            plan.Passes.Where(
                pass => pass.Name == "Lighting2D.NormalBuffer"));
        Assert.IsType<NormalBufferPassContext2D>(
    normalPass.ContextData);
        var output = Assert.Single(
            normalPass.ResourceAccesses.Where(
                access =>
                    access.Kind ==
                    RenderGraphResourceAccessKind.WriteColor));

        var resource = Assert.Single(
            plan.Resources.Where(
                resource => resource.Handle == output.Resource));

        Assert.Equal(
            "Lighting2D.NormalBuffer",
            resource.Name);

        Assert.Equal(
            new Vector4(0.5f, 0.5f, 1f, 1f),
            normalPass.ClearColorValue);

        Assert.True(normalPass.ExecuteQueuedCommands);

        Assert.Equal(
            RenderBlendMode.Alpha,
            normalPass.State.Blend.Mode);

        Assert.Equal(
            RenderBlendFactor.SourceAlpha,
            normalPass.State.Blend.SourceColorFactor);

        Assert.Equal(
            RenderBlendFactor.OneMinusSourceAlpha,
            normalPass.State.Blend.DestinationColorFactor);

        Assert.False(normalPass.State.Depth.TestEnabled);
        Assert.False(normalPass.State.Depth.WriteEnabled);

        var collectPass = Assert.Single(
            plan.Passes.Where(
                pass => pass.Name == "Lighting2D.Collect"));

        Assert.Contains(
            collectPass.Handle,
            normalPass.Dependencies);
    }

    [Fact]
    public void DirectLighting_ReadsNormalBuffer()
    {
        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(640, 480));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                sceneColor,
                RenderGraphLoadAction.Clear));

        var extension = CreateExtension();

        var context = new RenderGraphExtensionContext(
            640,
            480,
            0,
            RenderPass2D.World,
            sceneColor,
            sceneColor,
            backbuffer,
            worldPass);

        var result = extension.Build(
            builder,
            in context);

        builder.SetOutput(result.Output!.Value);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var normalBuffer = Assert.Single(
            plan.Resources.Where(
                resource => resource.Name ==
                    "Lighting2D.NormalBuffer"));

        var directLighting = Assert.Single(
            plan.Passes.Where(
                pass => pass.Name ==
                    "Lighting2D.DirectLighting"));

        Assert.Contains(
            directLighting.ResourceAccesses,
            access =>
                access.Kind == RenderGraphResourceAccessKind.Read &&
                access.Resource == normalBuffer.Handle);
    }

    [Fact]
    public void Composite_ReadsSceneAndAllLightingChannels()
    {
        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(640, 480));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                sceneColor,
                RenderGraphLoadAction.Clear));

        var extension = CreateExtension();

        var context = new RenderGraphExtensionContext(
            640,
            480,
            0,
            RenderPass2D.World,
            sceneColor,
            sceneColor,
            backbuffer,
            worldPass);

        var result = extension.Build(
            builder,
            in context);

        builder.SetOutput(result.Output!.Value);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var composite = Assert.Single(
            plan.Passes.Where(
                pass => pass.Name == "Lighting2D.Composite"));

        var reads = composite.ResourceAccesses
            .Where(access =>
                access.Kind == RenderGraphResourceAccessKind.Read)
            .ToArray();

        Assert.Equal(5, reads.Length);

        Assert.Contains(
            reads,
            access => access.Resource == sceneColor);

        var outputAccess = Assert.Single(
            composite.ResourceAccesses.Where(
                access => access.Kind ==
                    RenderGraphResourceAccessKind.WriteColor));

        Assert.Equal(
            result.Output.Value,
            outputAccess.Resource);
    }

    [Fact]
    public void Composite_UsesHdrLinearOutputWithoutDepth()
    {
        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(640, 480));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                sceneColor,
                RenderGraphLoadAction.Clear));

        var extension = CreateExtension();

        var context = new RenderGraphExtensionContext(
            640,
            480,
            0,
            RenderPass2D.World,
            sceneColor,
            sceneColor,
            backbuffer,
            worldPass);

        var result = extension.Build(
            builder,
            in context);

        builder.SetOutput(result.Output!.Value);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var output = Assert.Single(
            plan.Resources.Where(
                resource => resource.Handle == result.Output.Value));

        Assert.Equal(
            TextureFormat.Rgba16Float,
            output.Descriptor.Format);

        Assert.Equal(
            TextureColorSpace.Linear,
            output.Descriptor.ColorSpace);

        Assert.False(
            output.Descriptor.HasDepthAttachment);
    }

    [Fact]
    public void Composite_ReplacesPostProcessSource()
    {
        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(640, 480));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                sceneColor,
                RenderGraphLoadAction.Clear));

        var extension = CreateExtension();

        var context = new RenderGraphExtensionContext(
            640,
            480,
            0,
            RenderPass2D.World,
            sceneColor,
            sceneColor,
            backbuffer,
            worldPass);

        var result = extension.Build(
            builder,
            in context);

        builder.SetOutput(result.Output!.Value);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var composite = Assert.Single(
            plan.Passes.Where(
                pass => pass.Name == "Lighting2D.Composite"));

        Assert.True(composite.IsPostProcessSource);
    }

    [Fact]
    public void CompositeShader_ProvidesGlslAndHlslVariants()
    {
        var description =
            LightingCompositeShaderSource2D.CreateDescription();

        var glsl = description.GetVariant(ShaderLanguage.Glsl);
        var hlsl = description.GetVariant(ShaderLanguage.Hlsl);

        Assert.Contains(
            "ambient + direct + indirect",
            glsl.FragmentSource);

        Assert.Contains(
            "scene.rgb * combinedLighting + emission",
            glsl.FragmentSource);

        Assert.Contains(
            "ambient + direct + indirect",
            hlsl.FragmentSource);

        Assert.Contains(
            "scene.rgb * combinedLighting + emission",
            hlsl.FragmentSource);


    }

    private static LightingRenderGraphExtension2D CreateExtension()
    {
        return new LightingRenderGraphExtension2D(
            new LightingCore2D(),
            () => null!,
            new FakeLightingPassRenderer(),
            new FakeLightingCompositeRenderer());
    }

    private sealed class FakeLightingPassRenderer :
        ILightingPassRenderer2D
    {
        public void Render(
            in LightingPassContext2D context,
            RenderGraphContext renderGraphContext)
        {
        }
    }

    private sealed class FakeLightingCompositeRenderer :
        ILightingCompositeRenderer2D
    {
        public void Composite(
            in LightingCompositeContext2D context,
            RenderGraphContext renderGraphContext)
        {
        }
    }
}