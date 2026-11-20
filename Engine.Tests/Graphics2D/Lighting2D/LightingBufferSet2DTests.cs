using Engine.Core.Math;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Lighting;
using Xunit;

namespace Engine.Tests.Graphics2D.Lighting2D;

public sealed class LightingBufferSet2DTests
{
    [Fact]
    public void Create_UsesLinearHdrBuffersWithoutDepth()
    {
        var builder = new RenderGraphBuilder();

        var scene = builder.CreateTexture(
            "Scene",
            new RenderGraphResourceDescriptor(800, 600));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                scene,
                RenderGraphLoadAction.Clear));

        var buffers = LightingBufferSet2D.Create(
            builder,
            800,
            600);

        var lastPass = buffers.AddClearPasses(
            builder,
            worldPass);

        builder.SetOutput(scene);
        builder.SetOutput(buffers.DirectLighting);
        builder.SetOutput(buffers.AmbientLighting);
        builder.SetOutput(buffers.IndirectLighting);
        builder.SetOutput(buffers.Emission);
        builder.SetOutput(buffers.NormalBuffer);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var lightingResources = plan.Resources
            .Where(resource =>
                resource.Name.StartsWith(
                    "Lighting2D.",
                    StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(5, lightingResources.Length);

        var direct = Assert.Single(
            lightingResources.Where(
                resource => resource.Name ==
                    "Lighting2D.DirectLighting"));

        var ambient = Assert.Single(
            lightingResources.Where(
                resource => resource.Name ==
                    "Lighting2D.AmbientLighting"));

        var indirect = Assert.Single(
            lightingResources.Where(
                resource => resource.Name ==
                    "Lighting2D.IndirectLighting"));

        var emission = Assert.Single(
            lightingResources.Where(
                resource => resource.Name ==
                    "Lighting2D.Emission"));

        var normal = Assert.Single(
            lightingResources.Where(
                resource => resource.Name ==
                    "Lighting2D.NormalBuffer"));

        Assert.Equal(800, direct.Descriptor.Width);
        Assert.Equal(600, direct.Descriptor.Height);

        Assert.Equal(800, ambient.Descriptor.Width);
        Assert.Equal(600, ambient.Descriptor.Height);

        Assert.Equal(1, indirect.Descriptor.Width);
        Assert.Equal(1, indirect.Descriptor.Height);

        Assert.Equal(1, emission.Descriptor.Width);
        Assert.Equal(1, emission.Descriptor.Height);

        // Normal Buffer is full-resolution.
        Assert.Equal(800, normal.Descriptor.Width);
        Assert.Equal(600, normal.Descriptor.Height);

        Assert.NotEqual(
            buffers.IndirectLighting,
            buffers.Emission);

        Assert.NotEqual(
            indirect.Handle,
            emission.Handle);

        Assert.All(
            lightingResources,
            resource =>
            {
                Assert.Equal(
                    TextureFormat.Rgba16Float,
                    resource.Descriptor.Format);

                Assert.Equal(
                    TextureColorSpace.Linear,
                    resource.Descriptor.ColorSpace);

                Assert.False(
                    resource.Descriptor.HasDepthAttachment);
            });

        Assert.Contains(
            plan.Passes,
            pass => pass.Handle == lastPass);
    }

    [Fact]
    public void NormalBufferClear_UsesNeutralWorldNormal()
    {
        var builder = new RenderGraphBuilder();

        var scene = builder.CreateTexture(
            "Scene",
            new RenderGraphResourceDescriptor(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                scene,
                RenderGraphLoadAction.Clear));

        var buffers = LightingBufferSet2D.Create(
            builder,
            640,
            480);

        buffers.AddClearPasses(
            builder,
            worldPass);

        builder.SetOutput(scene);
        builder.SetOutput(buffers.NormalBuffer);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var normalClearPass = Assert.Single(
            plan.Passes.Where(
                pass => pass.Name ==
                    "Lighting2D.Clear.NormalBuffer"));

        Assert.Equal(
            new Vector4(0.5f, 0.5f, 1f, 1f),
            normalClearPass.ClearColorValue);
    }

    [Fact]
    public void GetResource_MapsEachChannel()
    {
        var builder = new RenderGraphBuilder();

        var buffers = LightingBufferSet2D.Create(
            builder,
            640,
            480);

        Assert.Equal(
            buffers.DirectLighting,
            buffers.GetResource(LightingChannel2D.Direct));

        Assert.Equal(
            buffers.AmbientLighting,
            buffers.GetResource(LightingChannel2D.Ambient));

        Assert.Equal(
            buffers.IndirectLighting,
            buffers.GetResource(LightingChannel2D.Indirect));

        Assert.Equal(
            buffers.Emission,
            buffers.GetResource(LightingChannel2D.Emission));
    }

    [Fact]
    public void GetResource_RejectsCombinedChannels()
    {
        var builder = new RenderGraphBuilder();

        var buffers = LightingBufferSet2D.Create(
            builder,
            640,
            480);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => buffers.GetResource(LightingChannel2D.All));
    }

    [Fact]
    public void AddClearPasses_PreservesPassOrder()
    {
        var builder = new RenderGraphBuilder();

        var scene = builder.CreateTexture(
            "Scene",
            new RenderGraphResourceDescriptor(640, 480));

        var worldPass = builder.AddPass(
            "World",
            pass => pass.WriteColor(
                scene,
                RenderGraphLoadAction.Clear));

        var buffers = LightingBufferSet2D.Create(
            builder,
            640,
            480);

        var lastPass = buffers.AddClearPasses(
            builder,
            worldPass);

        builder.SetOutput(scene);
        builder.SetOutput(buffers.DirectLighting);
        builder.SetOutput(buffers.AmbientLighting);
        builder.SetOutput(buffers.IndirectLighting);
        builder.SetOutput(buffers.Emission);
        builder.SetOutput(buffers.NormalBuffer);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var names = plan.Passes
            .Select(pass => pass.Name)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "World",
                "Lighting2D.Clear.DirectLighting",
                "Lighting2D.Clear.AmbientLighting",
                "Lighting2D.Clear.IndirectLighting",
                "Lighting2D.Clear.Emission",
                "Lighting2D.Clear.NormalBuffer"
            },
            names);

        Assert.Equal(
            plan.Passes[^1].Handle,
            lastPass);
    }

    [Fact]
    public void Create_RejectsInvalidDimensions()
    {
        var builder = new RenderGraphBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => LightingBufferSet2D.Create(
                builder,
                0,
                480));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => LightingBufferSet2D.Create(
                builder,
                640,
                0));
    }

    [Fact]
    public void Create_RejectsEmptyNamePrefix()
    {
        var builder = new RenderGraphBuilder();

        Assert.Throws<ArgumentException>(
            () => LightingBufferSet2D.Create(
                builder,
                640,
                480,
                ""));
    }
}