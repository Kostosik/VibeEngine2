using Engine.Core.Commands;

namespace Engine.Tests.Commands;

public sealed class CommandDispatcherTests
{
    [Fact]
    public void RegisterDoesNotExecuteHandler()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        var received =
            new List<int>();

        dispatcher.Register(
            new TestCommandHandler(received));

        Assert.Empty(received);
    }

    [Fact]
    public void DispatchPendingExecutesHandler()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        var received =
            new List<int>();

        dispatcher.Register(
            new TestCommandHandler(received));

        queue.Enqueue(
            new TestCommand(42));

        dispatcher.DispatchPending();

        Assert.Equal(
            new[]
            {
                42
            },
            received);

        Assert.Equal(
            0,
            queue.Count);
    }

    [Fact]
    public void CommandsAreExecutedInQueueOrder()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        var received =
            new List<int>();

        dispatcher.Register(
            new TestCommandHandler(received));

        queue.Enqueue(
            new TestCommand(1));

        queue.Enqueue(
            new TestCommand(2));

        queue.Enqueue(
            new TestCommand(3));

        dispatcher.DispatchPending();

        Assert.Equal(
            new[]
            {
                1,
                2,
                3
            },
            received);
    }

    [Fact]
    public void DifferentCommandTypesUseDifferentHandlers()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        var firstReceived =
            new List<int>();

        var secondReceived =
            new List<int>();

        dispatcher.Register(
            new TestCommandHandler(
                firstReceived));

        dispatcher.Register(
            new SecondTestCommandHandler(
                secondReceived));

        queue.Enqueue(
            new TestCommand(10));

        queue.Enqueue(
            new SecondTestCommand(20));

        dispatcher.DispatchPending();

        Assert.Equal(
            new[]
            {
                10
            },
            firstReceived);

        Assert.Equal(
            new[]
            {
                20
            },
            secondReceived);
    }

    [Fact]
    public void DuplicateHandlerThrows()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        dispatcher.Register(
            new TestCommandHandler(
                new List<int>()));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                dispatcher.Register(
                    new TestCommandHandler(
                        new List<int>()));
            });
    }

    [Fact]
    public void DisposedSubscriptionStopsHandler()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        var received =
            new List<int>();

        var subscription =
            dispatcher.Register(
                new TestCommandHandler(received));

        subscription.Dispose();

        queue.Enqueue(
            new TestCommand(42));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                dispatcher.DispatchPending();
            });

        Assert.Empty(received);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void MissingHandlerLeavesCommandInQueue()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        queue.Enqueue(
            new TestCommand(42));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                dispatcher.DispatchPending();
            });

        Assert.Equal(
            1,
            queue.Count);
    }

    [Fact]
    public void HandlerCanEnqueueAnotherCommand()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        var received =
            new List<int>();

        dispatcher.Register(
            new EnqueuingCommandHandler(
                queue,
                received));

        queue.Enqueue(
            new TestCommand(1));

        dispatcher.DispatchPending();

        Assert.Equal(
            new[]
            {
                1,
                2
            },
            received);

        Assert.Equal(
            0,
            queue.Count);
    }

    [Fact]
    public void ClearPendingRemovesQueuedCommands()
    {
        var queue =
            new CommandQueue();

        var dispatcher =
            new CommandDispatcher(queue);

        dispatcher.Register(
            new TestCommandHandler(
                new List<int>()));

        queue.Enqueue(
            new TestCommand(1));

        queue.Enqueue(
            new TestCommand(2));

        dispatcher.ClearPending();

        Assert.Equal(
            0,
            dispatcher.PendingCount);
    }
}