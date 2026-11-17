using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering.RenderGraph;

public sealed class RenderGraphCompilerTests
{
    [Fact]
    public void Compile_OrdersLiveImportedWritersByDeclarationOrder()
    {
        var builder = new RenderGraphBuilder();

        var imported = builder.ImportRenderTarget(
            "ImportedTexture",
            new RenderTargetHandle(1),
            new RenderTargetDescription(64, 64));

        var prerequisite = builder.CreateTexture(
            "Prerequisite",
            new RenderGraphResourceDescriptor(64, 64));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(64, 64));

        // Первый writer вынужден ждать ресурс,
        // который будет произведён позже.
        var earlierWriter = builder.AddPass(
            "EarlierWriter",
            pass => pass
                .Read(prerequisite)
                .WriteColor(imported, RenderGraphLoadAction.Clear));

        // Второй writer объявлен позже, но первоначально
        // не имеет зависимостей.
        var laterWriter = builder.AddPass(
            "LaterWriter",
            pass => pass.WriteColor(
                imported,
                RenderGraphLoadAction.Clear));

        var prerequisiteProducer = builder.AddPass(
            "PrerequisiteProducer",
            pass => pass.WriteColor(prerequisite));

        // Нужен для того, чтобы EarlierWriter остался живым
        // после culling.
        var anotherOutput = builder.AddPass(
            "AnotherOutput",
            pass => pass
                .DependsOn(earlierWriter)
                .WriteColor(backbuffer));

        builder.SetOutput(imported);
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Collection(
            plan.Passes,
            pass => Assert.Equal(prerequisiteProducer, pass.Handle),
            pass => Assert.Equal(earlierWriter, pass.Handle),
            pass => Assert.Equal(laterWriter, pass.Handle),
            pass => Assert.Equal(anotherOutput, pass.Handle));

        var compiledLaterWriter = plan.Passes.Single(
            pass => pass.Handle == laterWriter);

        Assert.Equal(
            new[] { earlierWriter },
            compiledLaterWriter.Dependencies);
    }

    [Fact]
    public void Compile_OrdersProducerBeforeConsumer_WhenProducerWasDeclaredLater()
    {
        var builder = new RenderGraphBuilder();
        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(1280, 720));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        var consumer = builder.AddPass(
            "PostProcess",
            pass => pass
                .Read(sceneColor)
                .WriteColor(backbuffer));

        var producer = builder.AddPass(
            "World",
            pass => pass.WriteColor(sceneColor));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Collection(
            plan.Passes,
            pass => Assert.Equal(producer, pass.Handle),
            pass => Assert.Equal(consumer, pass.Handle));

        Assert.Equal(
            new[] { producer },
            plan.Passes[1].Dependencies);
    }

    [Fact]
    public void Compile_RejectsTransientResourceWithMultipleWriters()
    {
        var builder = new RenderGraphBuilder();
        var texture = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(1280, 720));
        var output = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass("WorldA", pass => pass.WriteColor(texture));
        builder.AddPass("WorldB", pass => pass.WriteColor(texture));
        builder.AddPass(
            "Present",
            pass => pass.Read(texture).WriteColor(output));
        builder.SetOutput(output);

        var graph = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new RenderGraphCompiler().Compile(graph));

        Assert.Contains("multiple writers", exception.Message);
    }

    [Fact]
    public void Compile_RejectsTransientResourceReadWithoutProducer()
    {
        var builder = new RenderGraphBuilder();
        var texture = builder.CreateTexture(
            "MissingProducer",
            new RenderGraphResourceDescriptor(64, 64));
        var output = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(64, 64));

        builder.AddPass(
            "Present",
            pass => pass.Read(texture).WriteColor(output));
        builder.SetOutput(output);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new RenderGraphCompiler().Compile(builder.Build()));

        Assert.Contains("no producer", exception.Message);
    }

    [Fact]
    public void Compile_RejectsTransientOutputWithoutProducer()
    {
        var builder = new RenderGraphBuilder();
        var texture = builder.CreateTexture(
            "UnwrittenOutput",
            new RenderGraphResourceDescriptor(64, 64));

        builder.SetOutput(texture);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new RenderGraphCompiler().Compile(builder.Build()));

        Assert.Contains("no producer", exception.Message);
    }

    [Fact]
    public void Compile_RejectsCycleCreatedByResourceDependencies()
    {
        var builder = new RenderGraphBuilder();
        var textureA = builder.CreateTexture(
            "TextureA",
            new RenderGraphResourceDescriptor(64, 64));
        var textureB = builder.CreateTexture(
            "TextureB",
            new RenderGraphResourceDescriptor(64, 64));

        builder.AddPass(
            "PassA",
            pass => pass.Read(textureB).WriteColor(textureA));
        builder.AddPass(
            "PassB",
            pass => pass.Read(textureA).WriteColor(textureB));
        builder.SetOutput(textureA);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new RenderGraphCompiler().Compile(builder.Build()));

        Assert.Contains("dependency cycle", exception.Message);
    }

    [Fact]
    public void Compile_OrdersBackbufferLoadWritesInDeclarationOrder()
    {
        var builder = new RenderGraphBuilder();
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        var world = builder.AddPass(
            "World",
            pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Clear));
        var ui = builder.AddPass(
            "UI",
            pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Load));
        var debug = builder.AddPass(
            "Debug",
            pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Load));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Collection(
            plan.Passes,
            pass => Assert.Equal(world, pass.Handle),
            pass => Assert.Equal(ui, pass.Handle),
            pass => Assert.Equal(debug, pass.Handle));

        Assert.Equal(new[] { world }, plan.Passes[1].Dependencies);
        Assert.Equal(new[] { ui }, plan.Passes[2].Dependencies);
    }

    [Fact]
    public void Compile_ClearWriteSupersedesPreviousImportedWriter()
    {
        var builder = new RenderGraphBuilder();
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass(
            "UnusedFirstFrame",
            pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Clear));
        var finalPass = builder.AddPass(
            "FinalFrame",
            pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Clear));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        var compiledPass = Assert.Single(plan.Passes);
        Assert.Equal(finalPass, compiledPass.Handle);
        Assert.Equal(1, plan.CulledPassCount);
    }

    [Fact]
    public void Compile_CullsPassesThatDoNotContributeToOutputs()
    {
        var builder = new RenderGraphBuilder();
        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(1280, 720));
        var unusedTexture = builder.CreateTexture(
            "UnusedTexture",
            new RenderGraphResourceDescriptor(1280, 720));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass("World", pass => pass.WriteColor(sceneColor));
        builder.AddPass(
            "PostProcess",
            pass => pass.Read(sceneColor).WriteColor(backbuffer));
        builder.AddPass("UnusedPass", pass => pass.WriteColor(unusedTexture));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Equal(new[] { "World", "PostProcess" },
            plan.Passes.Select(pass => pass.Name));
        Assert.Equal(1, plan.CulledPassCount);
        Assert.DoesNotContain(
            plan.Resources,
            resource => resource.Name == "UnusedTexture");
    }

    [Fact]
    public void Compile_AllowsReadingImportedResourceBeforeGraphWritesIt()
    {
        var builder = new RenderGraphBuilder();
        var imported = builder.ImportRenderTarget(
            "ExternalScene",
            new RenderTargetHandle(12),
            new RenderTargetDescription(640, 480));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var passHandle = builder.AddPass(
            "Composite",
            pass => pass
                .Read(imported)
                .WriteColor(backbuffer));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        var pass = Assert.Single(plan.Passes);
        Assert.Equal(passHandle, pass.Handle);
        Assert.Empty(pass.Dependencies);
        Assert.Contains(plan.Resources, resource => resource.Name == "ExternalScene");
    }

    [Fact]
    public void Compile_RetainsExplicitDependencies()
    {
        var builder = new RenderGraphBuilder();
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var preparation = builder.AddPass("Preparation", _ => { });
        var output = builder.AddPass(
            "Output",
            pass => pass
                .DependsOn(preparation)
                .WriteColor(backbuffer));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Collection(
            plan.Passes,
            pass => Assert.Equal(preparation, pass.Handle),
            pass => Assert.Equal(output, pass.Handle));
        Assert.Equal(new[] { preparation }, plan.Passes[1].Dependencies);
    }

    [Fact]
    public void Compile_PreservesDeclarationOrderForIndependentPasses()
    {
        var builder = new RenderGraphBuilder();
        var outputA = builder.ImportBackbuffer(
            "OutputA",
            new RenderTargetDescription(64, 64));
        var outputB = builder.ImportRenderTarget(
            "OutputB",
            new RenderTargetHandle(2),
            new RenderTargetDescription(64, 64));

        var first = builder.AddPass(
            "First",
            pass => pass.WriteColor(outputA));
        var second = builder.AddPass(
            "Second",
            pass => pass.WriteColor(outputB));
        builder.SetOutput(outputA);
        builder.SetOutput(outputB);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Collection(
            plan.Passes,
            pass => Assert.Equal(first, pass.Handle),
            pass => Assert.Equal(second, pass.Handle));
    }

    [Fact]
    public void Compile_ReportsFirstAndLastUseForResources()
    {
        var builder = new RenderGraphBuilder();
        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(320, 200));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(320, 200));

        builder.AddPass("World", pass => pass.WriteColor(sceneColor));
        builder.AddPass("Lighting", pass => pass.Read(sceneColor).WriteColor(backbuffer));
        builder.AddPass("UI", pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Load));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        var sceneInfo = Assert.Single(plan.Resources, resource => resource.Name == "SceneColor");
        Assert.Equal(0, sceneInfo.FirstUseIndex);
        Assert.Equal(1, sceneInfo.LastUseIndex);
        Assert.False(sceneInfo.IsOutput);

        var backbufferInfo = Assert.Single(plan.Resources, resource => resource.Name == "Backbuffer");
        Assert.Equal(1, backbufferInfo.FirstUseIndex);
        Assert.Equal(2, backbufferInfo.LastUseIndex);
        Assert.True(backbufferInfo.IsOutput);
    }

    [Fact]
    public void Compile_AllowsImportedOutputWithoutGraphWriter()
    {
        var builder = new RenderGraphBuilder();
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(320, 200));
        builder.AddPass("Unused", _ => { });
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Empty(plan.Passes);
        Assert.Equal(1, plan.CulledPassCount);
        Assert.Contains(plan.Resources, resource => resource.IsOutput);
    }
    [Fact]
    public void Compile_PreservesReadsOfImportedContentsBeforeFirstOverwrite()
    {
        var builder = new RenderGraphBuilder();
        var imported = builder.ImportRenderTarget(
            "ExternalScene",
            new RenderTargetHandle(21),
            new RenderTargetDescription(640, 480));
        var captured = builder.CreateTexture(
            "CapturedScene",
            new RenderGraphResourceDescriptor(640, 480));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        var capture = builder.AddPass(
            "CaptureExternalScene",
            pass => pass.Read(imported).WriteColor(captured));
        var overwrite = builder.AddPass(
            "OverwriteExternalScene",
            pass => pass.WriteColor(imported, RenderGraphLoadAction.Clear));
        var composite = builder.AddPass(
            "CompositeCapturedScene",
            pass => pass.Read(captured).WriteColor(backbuffer));
        builder.SetOutput(backbuffer);
        builder.SetOutput(imported);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        Assert.Equal(
            new[] { "CaptureExternalScene", "OverwriteExternalScene", "CompositeCapturedScene" },
            plan.Passes.Select(pass => pass.Name));
        Assert.Equal(new[] { capture }, plan.Passes[1].Dependencies);
        Assert.Equal(new[] { capture }, plan.Passes[2].Dependencies);
        Assert.Contains(plan.Passes, pass => pass.Handle == overwrite);
        Assert.Contains(plan.Passes, pass => pass.Handle == composite);
    }

}
