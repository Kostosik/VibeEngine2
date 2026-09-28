using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;

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
    public void Execute_ExecutesCommandsInQueueOrder()
    {
        var queue = new RenderQueue();

        queue.Submit(
            new TestCommand(10));

        queue.Submit(
            new TestCommand(20));

        var pipeline = new RenderPipeline();

        pipeline.AddPass(
            RenderPass.Default2D);

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