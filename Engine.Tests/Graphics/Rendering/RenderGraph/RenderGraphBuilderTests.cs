using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering.RenderGraph;

public sealed class RenderGraphBuilderTests
{
    [Fact]
    public void Build_CreatesImmutableGraphDescription()
    {
        var builder = new RenderGraphBuilder();
        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(1280, 720));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass(
            "World",
            pass => pass.WriteColor(sceneColor));

        builder.AddPass(
            "Present",
            pass => pass
                .Read(sceneColor)
                .WriteColor(backbuffer));

        builder.SetOutput(backbuffer);

        var graph = builder.Build();

        Assert.Equal(2, graph.ResourceCount);
        Assert.Equal(2, graph.PassCount);
        Assert.Equal(backbuffer, Assert.Single(graph.Outputs));

        Assert.Throws<ObjectDisposedException>(
            () => builder.CreateTexture(
                "LateTexture",
                new RenderGraphResourceDescriptor(64, 64)));
    }

    [Fact]
    public void AddPass_RejectsResourceFromAnotherGraph()
    {
        var firstBuilder = new RenderGraphBuilder();
        var secondBuilder = new RenderGraphBuilder();

        var foreignResource = secondBuilder.CreateTexture(
            "Foreign",
            new RenderGraphResourceDescriptor(64, 64));

        Assert.Throws<ArgumentException>(
            () => firstBuilder.AddPass(
                "InvalidPass",
                pass => pass.Read(foreignResource)));
    }

    [Fact]
    public void AddPass_RejectsReadingAndWritingSameResource()
    {
        var builder = new RenderGraphBuilder();
        var texture = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(1280, 720));

        Assert.Throws<InvalidOperationException>(
            () => builder.AddPass(
                "FeedbackPass",
                pass => pass
                    .Read(texture)
                    .WriteColor(texture)));
    }

    [Fact]
    public void Build_RejectsLoadActionForTransientResource()
    {
        var builder = new RenderGraphBuilder();
        var texture = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(1280, 720));

        builder.AddPass(
            "World",
            pass => pass.WriteColor(
                texture,
                RenderGraphLoadAction.Load));

        builder.SetOutput(texture);

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void ImportRenderTarget_RejectsInvalidHandle()
    {
        var builder = new RenderGraphBuilder();

        Assert.Throws<ArgumentException>(
            () => builder.ImportRenderTarget(
                "ExternalTarget",
                RenderTargetHandle.Invalid,
                new RenderTargetDescription(1280, 720)));
    }

    [Fact]
    public void Build_RequiresAtLeastOneOutput()
    {
        var builder = new RenderGraphBuilder();

        Assert.Throws<InvalidOperationException>(builder.Build);
    }
}
