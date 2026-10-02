using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Rendering;
using Engine.Core.Math;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderPipelineTests
{

    [Fact]
    public void Execute_DoesNotExecuteCommandsOutsidePassRange()
    {
        var queue = new RenderQueue();

        queue.Submit(
            new TestCommand(
                50));

        queue.Submit(
            new TestCommand(
                1500));

        var pipeline = new RenderPipeline();

        pipeline.AddPass(
            new RenderPass(
                "UI",
                default,
                RenderState.Default2D,
                false,
                new RenderPassLayerRange(
                    RenderLayers.Ui,
                    RenderLayers.Debug - 1)));

        var executor =
            new TestExecutor();

        pipeline.Execute(
            queue,
            executor);

        var command =
            Assert.Single(
                executor.ExecutedCommands);

        Assert.Equal(
            1500,
            ((TestCommand)command).Id);
    }
    [Fact]
    public void Execute_FiltersCommandsByPassLayerRange()
    {
        var queue = new RenderQueue();

        queue.Submit(
            new TestCommand(10));

        queue.Submit(
            new TestCommand(100));

        var pipeline = new RenderPipeline();

        pipeline.AddPass(
            new RenderPass(
                "Low",
                default,
                RenderState.Default2D,
                false,
                new RenderPassLayerRange(
                    0,
                    50)));

        var executor = new TestExecutor();

        pipeline.Execute(
            queue,
            executor);

        var command =
            Assert.Single(
                executor.ExecutedCommands);

        Assert.Equal(
            10,
            ((TestCommand)command).Id);
    }

    [Fact]
    public void Execute_RunsEveryPass()
    {
        var queue = new RenderQueue();

        queue.Submit(
            new TestCommand(0));

        var pipeline = new RenderPipeline();

        pipeline.AddPass(
            new RenderPass(
                "First",
                default,
                RenderState.Default2D,
                false,
                RenderPassLayerRange.All));

        pipeline.AddPass(
            new RenderPass(
                "Second",
                default,
                RenderState.Default2D,
                false, new RenderPassLayerRange(0, 50)));

        var executor = new TestExecutor();

        pipeline.Execute(
            queue,
            executor);

        Assert.Equal(
            2,
            executor.BeginCount);

        Assert.Equal(
            2,
            executor.EndCount);

        Assert.Equal(
            2,
            executor.ExecutedCommands.Count);
    }

    [Fact]
    public void Execute_SupportsRenderTargetChain()
    {
        var targetA =
            new RenderTargetHandle(1);

        var targetB =
            new RenderTargetHandle(2);

        var queue =
            new RenderQueue();

        queue.Submit(
            new TestCommand(
                0));

        queue.Submit(
            new DrawRenderTargetCommand(
                targetA,
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new Rectangle(0, 0, 1, 1),
                900));

        queue.Submit(
            new DrawRenderTargetCommand(
                targetB,
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new Rectangle(0, 0, 1, 1),
                901));

        var pipeline =
            new RenderPipeline();

        pipeline.AddPass(
            new RenderPass(
                "Scene",
                targetA,
                RenderState.Default2D,
                true,
                new RenderPassLayerRange(
                    0,
                    899)));

        pipeline.AddPass(
            new RenderPass(
                "PostProcess",
                targetB,
                RenderState.Default2D,
                false,
                new RenderPassLayerRange(
                    900,
                    900)));

        pipeline.AddPass(
            new RenderPass(
                "Present",
                RenderTargetHandle.Invalid,
                RenderState.Default2D,
                false,
                new RenderPassLayerRange(
                    901,
                    901)));

        var executor =
            new TestExecutor();

        pipeline.Execute(
            queue,
            executor);

        Assert.Collection(
            executor.BegunPasses,
            pass => Assert.Equal(
                targetA,
                pass.Target),
            pass => Assert.Equal(
                targetB,
                pass.Target),
            pass => Assert.Equal(
                RenderTargetHandle.Invalid,
                pass.Target));

        Assert.Collection(
            executor.ExecutedCommands,
            command => Assert.IsType<TestCommand>(
                command),
            command =>
            {
                var renderTarget =
                    Assert.IsType<DrawRenderTargetCommand>(
                        command);

                Assert.Equal(
                    targetA,
                    renderTarget.Target);
            },
            command =>
            {
                var renderTarget =
                    Assert.IsType<DrawRenderTargetCommand>(
                        command);

                Assert.Equal(
                    targetB,
                    renderTarget.Target);
            });
    }

    [Fact]
    public void Execute_ExecutesCommandsInQueueOrder()
    {
        var queue = new RenderQueue();

        queue.Submit(
            new TestCommand(10));

        queue.Submit(
            new TestCommand(20));

        var pipeline = new RenderPipeline();

        pipeline.AddPass(
            RenderPass2D.Default);

        var executor = new TestExecutor();

        pipeline.Execute(
            queue,
            executor);

        Assert.Collection(
            executor.ExecutedCommands,
            command => Assert.Equal(
                10,
                ((TestCommand)command).Id),
            command => Assert.Equal(
                20,
                ((TestCommand)command).Id));
    }

    private sealed class TestExecutor
        : IRenderPassExecutor
    {
        public List<RenderPassContext> BegunPasses
        {
            get;
        } = new();

        public int BeginCount
        {
            get;
            private set;
        }

        public int EndCount
        {
            get;
            private set;
        }

        public List<IRenderCommand> ExecutedCommands
        {
            get;
        } = new();

        public void Begin(
            RenderPassContext pass)
        {
            BeginCount++;

            BegunPasses.Add(
                pass);
        }

        public void Execute(
            IRenderCommand command)
        {
            ExecutedCommands.Add(
                command);
        }

        public void End()
        {
            EndCount++;
        }
    }

    private sealed record TestCommand(
        int Id)
        : IRenderCommand
    {
        public int Layer =>
            Id;
    }
}