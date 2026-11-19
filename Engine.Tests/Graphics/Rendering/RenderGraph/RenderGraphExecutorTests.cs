using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering.RenderGraph;

public sealed class RenderGraphExecutorTests
{
    [Fact]
    public void Execute_DestroysTargetsBeyondPerDescriptionCacheLimit()
    {
        var targets = new TestRenderTargetManager();

        using var executor = new RenderGraphExecutor(
            targets,
            new TestRenderPassExecutor());

        var builder = new RenderGraphBuilder();

        var output = builder.CreateTexture(
            "Output",
            new RenderGraphResourceDescriptor(64, 64));

        builder.AddPass(
            "Produce",
            pass => pass.WriteColor(output));

        builder.SetOutput(output);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        // Удерживаем три результата одновременно,
        // чтобы пул получил три разных Render Target.
        var first = executor.Execute(plan);
        var second = executor.Execute(plan);
        var third = executor.Execute(plan);

        Assert.Equal(3, targets.CreateCount);

        first.Dispose();
        second.Dispose();
        third.Dispose();

        // В пуле остаются только два ресурса.
        Assert.Equal(1, targets.DestroyCount);
        Assert.Equal(2, targets.ActiveTargets.Count);

        // Проверяем повторное использование.
        using (executor.Execute(plan))
        {
        }

        Assert.Equal(3, targets.CreateCount);

        executor.Dispose();

        Assert.Equal(3, targets.DestroyCount);
        Assert.Empty(targets.ActiveTargets);
    }

    [Fact]
    public void Execute_DestroysTargetsBeyondGlobalCacheLimit()
    {
        var targets = new TestRenderTargetManager();

        using var executor = new RenderGraphExecutor(
            targets,
            new TestRenderPassExecutor());

        var results = new List<RenderGraphExecutionResult>();

        for (var i = 0; i < 17; i++)
        {
            var builder = new RenderGraphBuilder();

            var output = builder.CreateTexture(
                $"Output_{i}",
                new RenderGraphResourceDescriptor(64 + i, 64));

            builder.AddPass(
                "Produce",
                pass => pass.WriteColor(output));

            builder.SetOutput(output);

            var plan = new RenderGraphCompiler()
                .Compile(builder.Build());

            results.Add(executor.Execute(plan));
        }

        Assert.Equal(17, targets.CreateCount);

        // Возвращаем все 17 ресурсов в пул.
        foreach (var result in results)
        {
            result.Dispose();
        }

        // Максимальный размер пула — 16 ресурсов.
        Assert.Equal(1, targets.DestroyCount);
        Assert.Equal(16, targets.ActiveTargets.Count);

        executor.Dispose();

        Assert.Equal(17, targets.DestroyCount);
        Assert.Empty(targets.ActiveTargets);
    }

    [Fact]
    public void Dispose_DestroysTransientTargetsRetainedByExecutionResult()
    {
        var targets = new TestRenderTargetManager();

        using var executor = new RenderGraphExecutor(
            targets,
            new TestRenderPassExecutor());

        var builder = new RenderGraphBuilder();

        var output = builder.CreateTexture(
            "RetainedOutput",
            new RenderGraphResourceDescriptor(64, 64));

        builder.AddPass(
            "ProduceOutput",
            pass => pass.WriteColor(output));

        builder.SetOutput(output);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var result = executor.Execute(plan);

        var target = result.GetRenderTarget(output);

        Assert.True(targets.Exists(target));
        Assert.Single(targets.ActiveTargets);

        // Executor должен уничтожить даже удерживаемые output-ресурсы.
        executor.Dispose();

        Assert.False(targets.Exists(target));
        Assert.Empty(targets.ActiveTargets);

        // Результат должен безопасно освободиться после Executor.
        result.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => result.GetRenderTarget(output));
    }

    [Fact]
    public void ExecuteFullscreen_ResolvesAllDeclaredInputs()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestFullscreenPassExecutor();

        var builder = new RenderGraphBuilder();

        var description = new RenderTargetDescription(64, 64);

        var sceneTarget = targets.Create(description);
        var normalTarget = targets.Create(description);
        var lightTarget = targets.Create(description);
        var emissionTarget = targets.Create(description);

        var sceneColor = builder.ImportRenderTarget(
            "SceneColor", sceneTarget, description);

        var normalBuffer = builder.ImportRenderTarget(
            "NormalBuffer", normalTarget, description);

        var lightBuffer = builder.ImportRenderTarget(
            "LightBuffer", lightTarget, description);

        var emissionBuffer = builder.ImportRenderTarget(
            "EmissionBuffer", emissionTarget, description);

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer", description);

        var normalSampler = new TextureSamplerDescription(
            TextureFilter.Linear,
            TextureFilter.Linear,
            TextureWrapMode.Repeat,
            TextureWrapMode.MirroredRepeat);

        var fullscreenPass = new FullscreenPassDescription(
            new ShaderHandle(1),
            inputs:
            [
                new FullscreenPassTextureInput(
                "uSceneColor", sceneColor),

            new FullscreenPassTextureInput(
                "uNormalMap", normalBuffer, normalSampler),

            new FullscreenPassTextureInput(
                "uLightBuffer", lightBuffer),

            new FullscreenPassTextureInput(
                "uEmission", emissionBuffer)
            ]);

        builder.AddPass(
            "Lighting",
            pass => pass
                .Read(sceneColor)
                .Read(normalBuffer)
                .Read(lightBuffer)
                .Read(emissionBuffer)
                .WriteColor(backbuffer)
                .SetExecute(context =>
                    context.ExecuteFullscreen(fullscreenPass)));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var executor = new RenderGraphExecutor(
            targets,
            passExecutor);

        using var result = executor.Execute(plan);

        Assert.Equal(4, passExecutor.CapturedInputs.Count);

        Assert.Equal(
            targets.GetColorTexture(sceneTarget),
            passExecutor.CapturedInputs[0].Texture);

        Assert.Equal(
            targets.GetColorTexture(normalTarget),
            passExecutor.CapturedInputs[1].Texture);

        Assert.Equal(
            targets.GetColorTexture(lightTarget),
            passExecutor.CapturedInputs[2].Texture);

        Assert.Equal(
            targets.GetColorTexture(emissionTarget),
            passExecutor.CapturedInputs[3].Texture);

        Assert.Equal(
            normalSampler,
            passExecutor.CapturedInputs[1].Sampler);

        Assert.Equal(
            "uNormalMap",
            passExecutor.CapturedInputs[1].Name);
    }

    [Fact]
    public void ExecuteFullscreen_RejectsUndeclaredInput()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestFullscreenPassExecutor();

        var builder = new RenderGraphBuilder();

        var description = new RenderTargetDescription(64, 64);

        var targetA = targets.Create(description);
        var targetB = targets.Create(description);

        var inputA = builder.ImportRenderTarget(
            "InputA", targetA, description);

        var inputB = builder.ImportRenderTarget(
            "InputB", targetB, description);

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer", description);

        var fullscreenPass = new FullscreenPassDescription(
            new ShaderHandle(1),
            inputs:
            [
                new FullscreenPassTextureInput(
                "uInputB", inputB)
            ]);

        builder.AddPass(
            "Composite",
            pass => pass
                .Read(inputA)
                .WriteColor(backbuffer)
                .SetExecute(context =>
                    context.ExecuteFullscreen(fullscreenPass)));

        builder.SetOutput(backbuffer);
        builder.SetOutput(inputB);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var executor = new RenderGraphExecutor(
            targets,
            passExecutor);

        var exception = Assert.Throws<InvalidOperationException>(
            () => executor.Execute(plan));

        Assert.Contains(
            "did not declare Read access",
            exception.Message);
    }

    [Fact]
    public void Execute_FiltersQueueCommandsByPassLayerRange()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var queue = new RenderQueue();

        queue.Submit(new TestCommand(150));
        queue.Submit(new TestCommand(10));
        queue.Submit(new TestCommand(100));

        var builder = new RenderGraphBuilder();

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass(
            "World",
            pass => pass
                .WriteColor(backbuffer, RenderGraphLoadAction.Clear)
                .SetLayerRange(
                    new RenderPassLayerRange(0, 99)));

        builder.AddPass(
            "UI",
            pass => pass
                .WriteColor(backbuffer, RenderGraphLoadAction.Load)
                .SetLayerRange(
                    new RenderPassLayerRange(100, 199)));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var result = new RenderGraphExecutor(
            targets,
            passExecutor).Execute(plan, queue);

        Assert.Collection(
            passExecutor.ExecutedCommands,
            command => Assert.Equal(
                10,
                ((TestCommand)command).Layer),
            command => Assert.Equal(
                100,
                ((TestCommand)command).Layer),
            command => Assert.Equal(
                150,
                ((TestCommand)command).Layer));
    }

    [Fact]
    public void Execute_ForwardsPassContextDataToRenderPassContext()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var builder = new RenderGraphBuilder();

        var contextData = new TestContextData();

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass(
            "World",
            pass => pass
                .WriteColor(backbuffer)
                .SetContextData(contextData));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var result = new RenderGraphExecutor(
            targets,
            passExecutor).Execute(plan);

        var begunPass = Assert.Single(passExecutor.BegunPasses);

        Assert.Same(contextData, begunPass.ContextData);
    }

    [Fact]
    public void Execute_RunsProducerBeforeConsumerAndResolvesTexture()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var builder = new RenderGraphBuilder();

        var sceneColor = builder.CreateTexture(
            "SceneColor",
            new RenderGraphResourceDescriptor(1280, 720));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        var events = new List<string>();
        TextureHandle? sampledTexture = null;

        builder.AddPass(
            "PostProcess",
            pass => pass
                .Read(sceneColor)
                .WriteColor(backbuffer)
                .SetExecute(context =>
                {
                    events.Add("PostProcess");
                    sampledTexture = context.GetTexture(sceneColor);
                }));

        builder.AddPass(
            "World",
            pass => pass
                .WriteColor(sceneColor)
                .SetExecute(context =>
                {
                    events.Add("World");
                    context.Execute(new TestCommand(10));
                }));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());
        var executor = new RenderGraphExecutor(targets, passExecutor);
        var result = executor.Execute(plan);

        Assert.Equal(new[] { "World", "PostProcess" }, events);
        Assert.True(sampledTexture.HasValue);
        Assert.True(sampledTexture.Value.IsValid);
        Assert.Equal(2, passExecutor.BeginCount);
        Assert.Equal(2, passExecutor.EndCount);
        Assert.Single(passExecutor.ExecutedCommands);
        Assert.Equal(RenderTargetHandle.Invalid, result.GetRenderTarget(backbuffer));

        result.Dispose();

        Assert.Single(targets.ActiveTargets);

        // Освобождаем весь пул.
        executor.Dispose();

        Assert.Empty(targets.ActiveTargets);
    }

    [Fact]
    public void Execute_MapsColorAndDepthClearsIndependently()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var builder = new RenderGraphBuilder();

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass(
            "ColorClear",
            pass => pass.WriteColor(
                backbuffer,
                RenderGraphLoadAction.Clear));

        builder.AddPass(
            "DepthClear",
            pass => pass
                .WriteColor(backbuffer, RenderGraphLoadAction.Load)
                .SetClearDepth());

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var result = new RenderGraphExecutor(
            targets,
            passExecutor).Execute(plan);

        Assert.Collection(
            passExecutor.BegunPasses,
            context =>
            {
                Assert.True(context.ClearColor);
                Assert.False(context.ClearDepth);
            },
            context =>
            {
                Assert.False(context.ClearColor);
                Assert.True(context.ClearDepth);
            });
    }

    [Fact]
    public void Execute_ReusesTargetForNonOverlappingTransientResources()
    {
        var targets = new TestRenderTargetManager();
        using var executor = new RenderGraphExecutor(
            targets,
            new TestRenderPassExecutor());

        var builder = new RenderGraphBuilder();

        var firstTexture = builder.CreateTexture(
            "First",
            new RenderGraphResourceDescriptor(320, 200));

        var secondTexture = builder.CreateTexture(
            "Second",
            new RenderGraphResourceDescriptor(320, 200));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(320, 200));

        var firstTarget = RenderTargetHandle.Invalid;
        var secondTarget = RenderTargetHandle.Invalid;

        builder.AddPass(
            "ProduceFirst",
            pass => pass
                .WriteColor(firstTexture)
                .SetExecute(context =>
                    firstTarget = context.ColorTarget));

        builder.AddPass(
            "ConsumeFirst",
            pass => pass
                .Read(firstTexture)
                .WriteColor(backbuffer));

        builder.AddPass(
            "ProduceSecond",
            pass => pass
                .WriteColor(secondTexture)
                .SetExecute(context =>
                    secondTarget = context.ColorTarget));

        builder.AddPass(
            "ConsumeSecond",
            pass => pass
                .Read(secondTexture)
                .WriteColor(backbuffer, RenderGraphLoadAction.Load));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var result = executor.Execute(plan);

        Assert.True(firstTarget.IsValid);
        Assert.Equal(firstTarget, secondTarget);

        Assert.Equal(1, targets.CreateCount);

        var firstResource = Assert.Single(
            plan.Resources,
            resource => resource.Name == "First");

        var secondResource = Assert.Single(
            plan.Resources,
            resource => resource.Name == "Second");

        Assert.Equal(0, firstResource.FirstUseIndex);
        Assert.Equal(1, firstResource.LastUseIndex);

        Assert.Equal(2, secondResource.FirstUseIndex);
        Assert.Equal(3, secondResource.LastUseIndex);
    }

    [Fact]
    public void Execute_DoesNotReuseTargetForOverlappingTransientResources()
    {
        var targets = new TestRenderTargetManager();

        using var executor = new RenderGraphExecutor(
            targets,
            new TestRenderPassExecutor());

        var builder = new RenderGraphBuilder();

        var firstTexture = builder.CreateTexture(
            "First",
            new RenderGraphResourceDescriptor(320, 200));

        var secondTexture = builder.CreateTexture(
            "Second",
            new RenderGraphResourceDescriptor(320, 200));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(320, 200));

        var firstTarget = RenderTargetHandle.Invalid;
        var secondTarget = RenderTargetHandle.Invalid;

        builder.AddPass(
            "ProduceFirst",
            pass => pass
                .WriteColor(firstTexture)
                .SetExecute(context =>
                    firstTarget = context.ColorTarget));

        builder.AddPass(
            "ProduceSecond",
            pass => pass
                .WriteColor(secondTexture)
                .SetExecute(context =>
                    secondTarget = context.ColorTarget));

        builder.AddPass(
            "ConsumeBoth",
            pass => pass
                .Read(firstTexture)
                .Read(secondTexture)
                .WriteColor(backbuffer));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler()
            .Compile(builder.Build());

        using var result = executor.Execute(plan);

        Assert.True(firstTarget.IsValid);
        Assert.True(secondTarget.IsValid);

        Assert.NotEqual(firstTarget, secondTarget);
        Assert.Equal(2, targets.CreateCount);

        var firstResource = Assert.Single(
            plan.Resources,
            resource => resource.Name == "First");

        var secondResource = Assert.Single(
            plan.Resources,
            resource => resource.Name == "Second");

        Assert.Equal(0, firstResource.FirstUseIndex);
        Assert.Equal(2, firstResource.LastUseIndex);

        Assert.Equal(1, secondResource.FirstUseIndex);
        Assert.Equal(2, secondResource.LastUseIndex);
    }

    [Fact]
    public void Execute_ReleasesTransientAfterLastUse()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var builder = new RenderGraphBuilder();

        var intermediate = builder.CreateTexture(
            "Intermediate",
            new RenderGraphResourceDescriptor(320, 200));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(320, 200));

        RenderTargetHandle createdTarget = RenderTargetHandle.Invalid;

        builder.AddPass(
            "Producer",
            pass => pass
                .WriteColor(intermediate)
                .SetExecute(context => createdTarget = context.ColorTarget));
        builder.AddPass(
            "Consumer",
            pass => pass.Read(intermediate).WriteColor(backbuffer));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        var executor = new RenderGraphExecutor(targets, passExecutor);
        var result = executor.Execute(plan);

        Assert.True(createdTarget.IsValid);
        Assert.True(targets.Exists(createdTarget));

        result.Dispose();

        // Ресурс находится в пуле и ожидает повторного использования.
        Assert.True(targets.Exists(createdTarget));

        executor.Dispose();

        // Теперь ресурс действительно уничтожен.
        Assert.False(targets.Exists(createdTarget));
        Assert.Empty(targets.ActiveTargets);
    }

    [Fact]
    public void Execute_RetainsTransientOutputUntilResultIsDisposed()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var executor = new RenderGraphExecutor(targets, passExecutor);

        var builder = new RenderGraphBuilder();

        var output = builder.CreateTexture(
            "CapturedOutput",
            new RenderGraphResourceDescriptor(64, 64));

        builder.AddPass("Capture", pass => pass.WriteColor(output));
        builder.SetOutput(output);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        var result = executor.Execute(plan);
        var target = result.GetRenderTarget(output);

        // Пока результат не освобождён, ресурс занят.
        Assert.True(targets.Exists(target));
        Assert.True(result.GetTexture(output).IsValid);

        result.Dispose();

        Assert.True(targets.Exists(target));
        Assert.Throws<ObjectDisposedException>(() => result.GetRenderTarget(output));

        // Следующее выполнение должно использовать тот же RenderTarget.
        using (var secondResult = executor.Execute(plan))
        {
            Assert.Equal(target, secondResult.GetRenderTarget(output));
        }

        // Теперь окончательно освобождаем пул.
        executor.Dispose();

        Assert.False(targets.Exists(target));
        Assert.Empty(targets.ActiveTargets);
    }

    [Fact]
    public void Execute_DoesNotDestroyImportedRenderTarget()
    {
        var targets = new TestRenderTargetManager();
        var externalTarget = targets.Create(new RenderTargetDescription(640, 480));
        var passExecutor = new TestRenderPassExecutor();
        var builder = new RenderGraphBuilder();

        var imported = builder.ImportRenderTarget(
            "ExternalScene",
            externalTarget,
            new RenderTargetDescription(640, 480));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(640, 480));

        builder.AddPass(
            "Composite",
            pass => pass
                .Read(imported)
                .WriteColor(backbuffer)
                .SetExecute(context => Assert.Equal(
                    targets.GetColorTexture(externalTarget),
                    context.GetTexture(imported))));
        builder.SetOutput(backbuffer);
        builder.SetOutput(imported);

        var plan = new RenderGraphCompiler().Compile(builder.Build());
        using var result = new RenderGraphExecutor(targets, passExecutor).Execute(plan);

        Assert.True(targets.Exists(externalTarget));
        Assert.Equal(externalTarget, result.GetRenderTarget(imported));
    }

    [Fact]
    public void Execute_EndsPassAndReleasesTransientWhenCallbackThrows()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var builder = new RenderGraphBuilder();
        var executor = new RenderGraphExecutor(targets, passExecutor);
        var output = builder.CreateTexture(
            "Output",
            new RenderGraphResourceDescriptor(128, 128));
        builder.AddPass(
            "FailingPass",
            pass => pass
                .WriteColor(output)
                .SetExecute(_ => throw new InvalidOperationException("Pass failed.")));
        builder.SetOutput(output);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        var exception = Assert.Throws<InvalidOperationException>(
            () => executor.Execute(plan));

        Assert.Equal("Pass failed.", exception.Message);
        Assert.Equal(1, passExecutor.BeginCount);
        Assert.Equal(1, passExecutor.EndCount);
        // Ресурс освобождён и возвращён в пул.
        Assert.Single(targets.ActiveTargets);

        // Окончательно уничтожаем ресурсы пула.
        executor.Dispose();

        Assert.Empty(targets.ActiveTargets);
    }

    [Fact]
    public void Execute_MapsLoadActionToClearColorFlag()
    {
        var targets = new TestRenderTargetManager();
        var passExecutor = new TestRenderPassExecutor();
        var builder = new RenderGraphBuilder();
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(1280, 720));

        builder.AddPass(
            "World",
            pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Clear));
        builder.AddPass(
            "UI",
            pass => pass.WriteColor(backbuffer, RenderGraphLoadAction.Load));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());
        using var result = new RenderGraphExecutor(targets, passExecutor).Execute(plan);

        Assert.Collection(
            passExecutor.BegunPasses,
            context => Assert.True(context.ClearColor),
            context => Assert.False(context.ClearColor));
    }

    [Fact]
    public void Execute_RejectsReadingResourceNotDeclaredByPass()
    {
        var targets = new TestRenderTargetManager();
        var importedA = targets.Create(new RenderTargetDescription(64, 64));
        var importedB = targets.Create(new RenderTargetDescription(64, 64));
        var builder = new RenderGraphBuilder();

        var inputA = builder.ImportRenderTarget(
            "InputA",
            importedA,
            new RenderTargetDescription(64, 64));
        var inputB = builder.ImportRenderTarget(
            "InputB",
            importedB,
            new RenderTargetDescription(64, 64));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(64, 64));

        builder.AddPass(
            "Composite",
            pass => pass
                .Read(inputA)
                .WriteColor(backbuffer)
                .SetExecute(context => context.GetTexture(inputB)));
        builder.SetOutput(backbuffer);
        builder.SetOutput(inputB);

        var plan = new RenderGraphCompiler().Compile(builder.Build());
        var exception = Assert.Throws<InvalidOperationException>(
            () => new RenderGraphExecutor(targets, new TestRenderPassExecutor()).Execute(plan));

        Assert.Contains("did not declare Read access", exception.Message);
        Assert.True(targets.Exists(importedA));
        Assert.True(targets.Exists(importedB));
    }

    [Fact]
    public void Execute_RejectsImportedTargetWithMismatchedDescription()
    {
        var targets = new TestRenderTargetManager();
        var externalTarget = targets.Create(new RenderTargetDescription(64, 64));
        var builder = new RenderGraphBuilder();
        var imported = builder.ImportRenderTarget(
            "External",
            externalTarget,
            new RenderTargetDescription(128, 128));
        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(128, 128));

        builder.AddPass(
            "Copy",
            pass => pass.Read(imported).WriteColor(backbuffer));
        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        var exception = Assert.Throws<InvalidOperationException>(
            () => new RenderGraphExecutor(targets, new TestRenderPassExecutor()).Execute(plan));

        Assert.Contains("expects", exception.Message);
        Assert.True(targets.Exists(externalTarget));
    }

    private sealed record TestCommand(int Layer) : IRenderCommand;

    private sealed class TestRenderPassExecutor : IRenderPassExecutor
    {
        public List<RenderPassContext> BegunPasses { get; } = [];
        public List<IRenderCommand> ExecutedCommands { get; } = [];
        public int BeginCount { get; private set; }
        public int EndCount { get; private set; }

        public void Begin(RenderPassContext context)
        {
            BeginCount++;
            BegunPasses.Add(context);
        }

        public void Execute(IRenderCommand command)
        {
            ExecutedCommands.Add(command);
        }

        public void End()
        {
            EndCount++;
        }
    }

    [Fact]
    public void Execute_ReusesTransientTargetAcrossExecutions()
    {
        var targets = new TestRenderTargetManager();
        var executor = new RenderGraphExecutor(
            targets,
            new TestRenderPassExecutor());

        var builder = new RenderGraphBuilder();

        var intermediate = builder.CreateTexture(
            "Intermediate",
            new RenderGraphResourceDescriptor(320, 200));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(320, 200));

        var firstTarget = RenderTargetHandle.Invalid;
        var secondTarget = RenderTargetHandle.Invalid;

        builder.AddPass(
            "Producer",
            pass => pass
                .WriteColor(intermediate)
                .SetExecute(context =>
                {
                    if (!firstTarget.IsValid)
                    {
                        firstTarget = context.ColorTarget;
                    }
                    else
                    {
                        secondTarget = context.ColorTarget;
                    }
                }));

        builder.AddPass(
            "Consumer",
            pass => pass
                .Read(intermediate)
                .WriteColor(backbuffer));

        builder.SetOutput(backbuffer);

        var plan = new RenderGraphCompiler().Compile(builder.Build());

        using (executor.Execute(plan))
        {
        }

        using (executor.Execute(plan))
        {
        }

        Assert.True(firstTarget.IsValid);
        Assert.Equal(firstTarget, secondTarget);
        Assert.Equal(1, targets.CreateCount);
        Assert.Equal(0, targets.DestroyCount);

        executor.Dispose();

        Assert.Equal(1, targets.DestroyCount);
        Assert.Empty(targets.ActiveTargets);
    }

    private sealed class TestRenderTargetManager : IRenderTargetManager
    {
        private readonly Dictionary<RenderTargetHandle, Entry> _targets = [];
        private uint _nextTarget = 1;
        private uint _nextTexture = 100;
        public int CreateCount { get; private set; }

        public int DestroyCount { get; private set; }
        public IReadOnlyCollection<RenderTargetHandle> ActiveTargets =>
            _targets.Keys.ToArray();

        public RenderTargetHandle Create(RenderTargetDescription description)
        {
            description.Validate();

            CreateCount++;

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
            if (_targets.Remove(target))
            {
                DestroyCount++;
            }
        }

        public void Resize(RenderTargetHandle target, int width, int height)
        {
            var entry = GetEntry(target);
            var description = new RenderTargetDescription(
                width,
                height,
                entry.Description.Format);
            description.Validate();
            _targets[target] = entry with { Description = description };
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
    private sealed class TestFullscreenPassExecutor :
    IRenderPassExecutor,
    IFullscreenPassExecutor
    {
        public IReadOnlyList<FullscreenPassTextureBinding>
            CapturedInputs
        { get; private set; } = [];

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
        }
    }
    private sealed class TestContextData : IRenderPassContextData;
}
