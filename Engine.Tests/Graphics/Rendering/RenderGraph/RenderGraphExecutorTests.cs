using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering.RenderGraph;

public sealed class RenderGraphExecutorTests
{

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
        Assert.Equal(1, passExecutor.ExecutedCommands.Count);
        Assert.Equal(RenderTargetHandle.Invalid, result.GetRenderTarget(backbuffer));

        result.Dispose();

        Assert.Single(targets.ActiveTargets);

        // Освобождаем весь пул.
        executor.Dispose();

        Assert.Empty(targets.ActiveTargets);
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
        public List<RenderPassContext> BegunPasses { get; } = new();
        public List<IRenderCommand> ExecutedCommands { get; } = new();
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
        private readonly Dictionary<RenderTargetHandle, Entry> _targets = new();
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
}
