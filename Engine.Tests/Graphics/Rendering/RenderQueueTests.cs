using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Core.Math;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderQueueTests
{
    [Fact]
    public void Sort_OrdersByLayerThenSubmissionOrder()
    {
        var queue =
            new RenderQueue();

        var layerTwo =
            new TestCommand(
                layer: 2);

        var layerZeroFirst =
            new TestCommand(
                layer: 0);

        var layerOne =
            new TestCommand(
                layer: 1);

        var layerZeroSecond =
            new TestCommand(
                layer: 0);

        queue.Submit(
            layerTwo);

        queue.Submit(
            layerZeroFirst);

        queue.Submit(
            layerOne);

        queue.Submit(
            layerZeroSecond);

        queue.Sort();

        Assert.Collection(
            queue.Items,

            item =>
            {
                Assert.Same(
                    layerZeroFirst,
                    item.Command);
                Assert.Equal(
                    0,
                    item.Layer);
                Assert.Equal(
                    1,
                    item.Order);
            },

            item =>
            {
                Assert.Same(
                    layerZeroSecond,
                    item.Command);
                Assert.Equal(
                    0,
                    item.Layer);
                Assert.Equal(
                    3,
                    item.Order);
            },

            item =>
            {
                Assert.Same(
                    layerOne,
                    item.Command);
                Assert.Equal(
                    1,
                    item.Layer);
                Assert.Equal(
                    2,
                    item.Order);
            },

            item =>
            {
                Assert.Same(
                    layerTwo,
                    item.Command);
                Assert.Equal(
                    2,
                    item.Layer);
                Assert.Equal(
                    0,
                    item.Order);
            });
    }

    [Fact]
    public void Clear_RemovesCommandsAndResetsSubmissionOrder()
    {
        var queue =
            new RenderQueue();

        queue.Submit(
            new TestCommand(0));

        queue.Clear();

        Assert.Equal(
            0,
            queue.Count);

        var command =
            new TestCommand(0);

        queue.Submit(
            command);

        Assert.Equal(
            0,
            queue.Items[0].Order);
    }

    private sealed class TestCommand :
        IRenderCommand
    {
        public TestCommand(
            int layer)
        {
            Layer = layer;
        }

        public int Layer { get; }

        public Vector2 Position =>
            Vector2.Zero;
    }
}