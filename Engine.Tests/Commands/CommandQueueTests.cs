using Engine.Core.Commands;

namespace Engine.Tests.Commands;

public sealed class CommandQueueTests
{
    private readonly record struct TestCommand(int Value)
        : ICommand;

    [Fact]
    public void Enqueue_AddsCommand()
    {
        var queue = new CommandQueue();

        queue.Enqueue(new TestCommand(42));

        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void TryDequeue_ReturnsCommand()
    {
        var queue = new CommandQueue();

        var command = new TestCommand(42);

        queue.Enqueue(command);

        var result = queue.TryDequeue(
            out var dequeued);

        Assert.True(result);
        Assert.Equal(command, dequeued);
    }

    [Fact]
    public void TryDequeue_PreservesOrder()
    {
        var queue = new CommandQueue();

        queue.Enqueue(new TestCommand(1));
        queue.Enqueue(new TestCommand(2));
        queue.Enqueue(new TestCommand(3));

        Assert.True(
            queue.TryDequeue(out var first));

        Assert.True(
            queue.TryDequeue(out var second));

        Assert.True(
            queue.TryDequeue(out var third));

        Assert.Equal(
            new TestCommand(1),
            first);

        Assert.Equal(
            new TestCommand(2),
            second);

        Assert.Equal(
            new TestCommand(3),
            third);
    }

    [Fact]
    public void TryPeekReturnsFirstCommandWithoutRemovingIt()
    {
        var queue =
            new CommandQueue();

        var command =
            new TestCommand(42);

        queue.Enqueue(command);

        var found =
            queue.TryPeek(
                out var peeked);

        Assert.True(found);
        Assert.NotNull(peeked);
        Assert.Equal(command, peeked);
        Assert.Equal(
            1,
            queue.Count);
    }

    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsFalse()
    {
        var queue = new CommandQueue();

        var result = queue.TryDequeue(
            out var command);

        Assert.False(result);
        Assert.Null(command);
    }

    [Fact]
    public void Clear_RemovesAllCommands()
    {
        var queue = new CommandQueue();

        queue.Enqueue(new TestCommand(1));
        queue.Enqueue(new TestCommand(2));

        queue.Clear();

        Assert.Equal(0, queue.Count);
    }
}