using Engine.Core.Events;

namespace Engine.Tests.Events;

public sealed class EventBusTests
{
    [Fact]
    public void PublishAddsPendingEvent()
    {
        var eventBus =
            new EventBus();

        eventBus.Publish(
            new TestEvent(42));

        Assert.Equal(
            1,
            eventBus.PendingCount);
    }

    [Fact]
    public void PublishDoesNotInvokeHandlerImmediately()
    {
        var eventBus =
            new EventBus();

        var received =
            new List<int>();

        eventBus.Subscribe<TestEvent>(
            @event =>
                received.Add(
                    @event.Value));

        eventBus.Publish(
            new TestEvent(42));

        Assert.Empty(
            received);
    }

    [Fact]
    public void DispatchPendingInvokesHandler()
    {
        var eventBus =
            new EventBus();

        var received =
            new List<int>();

        eventBus.Subscribe<TestEvent>(
            @event =>
                received.Add(
                    @event.Value));

        eventBus.Publish(
            new TestEvent(42));

        eventBus.DispatchPending();

        Assert.Equal(
            new[]
            {
                42
            },
            received);

        Assert.Equal(
            0,
            eventBus.PendingCount);
    }

    [Fact]
    public void MultipleHandlersReceiveEvent()
    {
        var eventBus =
            new EventBus();

        var first =
            new List<int>();

        var second =
            new List<int>();

        eventBus.Subscribe<TestEvent>(
            @event =>
                first.Add(
                    @event.Value));

        eventBus.Subscribe<TestEvent>(
            @event =>
                second.Add(
                    @event.Value));

        eventBus.Publish(
            new TestEvent(10));

        eventBus.DispatchPending();

        Assert.Equal(
            new[]
            {
                10
            },
            first);

        Assert.Equal(
            new[]
            {
                10
            },
            second);
    }

    [Fact]
    public void EventsAreDispatchedInPublishOrder()
    {
        var eventBus =
            new EventBus();

        var received =
            new List<int>();

        eventBus.Subscribe<TestEvent>(
            @event =>
                received.Add(
                    @event.Value));

        eventBus.Publish(
            new TestEvent(1));

        eventBus.Publish(
            new TestEvent(2));

        eventBus.Publish(
            new TestEvent(3));

        eventBus.DispatchPending();

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
    public void DifferentEventTypesUseDifferentHandlers()
    {
        var eventBus =
            new EventBus();

        var first =
            new List<int>();

        var second =
            new List<int>();

        eventBus.Subscribe<TestEvent>(
            @event =>
                first.Add(
                    @event.Value));

        eventBus.Subscribe<SecondTestEvent>(
            @event =>
                second.Add(
                    @event.Value));

        eventBus.Publish(
            new TestEvent(10));

        eventBus.Publish(
            new SecondTestEvent(20));

        eventBus.DispatchPending();

        Assert.Equal(
            new[]
            {
                10
            },
            first);

        Assert.Equal(
            new[]
            {
                20
            },
            second);
    }

    [Fact]
    public void DisposedSubscriptionStopsHandler()
    {
        var eventBus =
            new EventBus();

        var received =
            new List<int>();

        var subscription =
            eventBus.Subscribe<TestEvent>(
                @event =>
                    received.Add(
                        @event.Value));

        subscription.Dispose();

        eventBus.Publish(
            new TestEvent(42));

        eventBus.DispatchPending();

        Assert.Empty(
            received);
    }

    [Fact]
    public void DisposingOneSubscriptionDoesNotAffectOtherHandlers()
    {
        var eventBus =
            new EventBus();

        var first =
            new List<int>();

        var second =
            new List<int>();

        var firstSubscription =
            eventBus.Subscribe<TestEvent>(
                @event =>
                    first.Add(
                        @event.Value));

        eventBus.Subscribe<TestEvent>(
            @event =>
                second.Add(
                    @event.Value));

        firstSubscription.Dispose();

        eventBus.Publish(
            new TestEvent(42));

        eventBus.DispatchPending();

        Assert.Empty(
            first);

        Assert.Equal(
            new[]
            {
                42
            },
            second);
    }

    [Fact]
    public void EventWithoutHandlersIsIgnored()
    {
        var eventBus =
            new EventBus();

        eventBus.Publish(
            new TestEvent(42));

        eventBus.DispatchPending();

        Assert.Equal(
            0,
            eventBus.PendingCount);
    }

    [Fact]
    public void ClearPendingRemovesEvents()
    {
        var eventBus =
            new EventBus();

        eventBus.Publish(
            new TestEvent(1));

        eventBus.Publish(
            new TestEvent(2));

        eventBus.ClearPending();

        Assert.Equal(
            0,
            eventBus.PendingCount);
    }

    [Fact]
    public void EventPublishedDuringDispatchIsProcessed()
    {
        var eventBus =
            new EventBus();

        var received =
            new List<int>();

        eventBus.Subscribe<TestEvent>(
            @event =>
            {
                received.Add(
                    @event.Value);

                if (@event.Value == 1)
                {
                    eventBus.Publish(
                        new TestEvent(2));
                }
            });

        eventBus.Publish(
            new TestEvent(1));

        eventBus.DispatchPending();

        Assert.Equal(
            new[]
            {
                1,
                2
            },
            received);

        Assert.Equal(
            0,
            eventBus.PendingCount);
    }

    [Fact]
    public void HandlerCanDisposeItsOwnSubscription()
    {
        var eventBus =
            new EventBus();

        var received =
            new List<int>();

        EventSubscription? subscription = null;

        subscription =
            eventBus.Subscribe<TestEvent>(
                @event =>
                {
                    received.Add(
                        @event.Value);

                    subscription!.Dispose();
                });

        eventBus.Publish(
            new TestEvent(1));

        eventBus.Publish(
            new TestEvent(2));

        eventBus.DispatchPending();

        Assert.Equal(
            new[]
            {
                1
            },
            received);
    }

    [Fact]
    public void DisposingSubscriptionTwiceIsSafe()
    {
        var eventBus =
            new EventBus();

        var subscription =
            eventBus.Subscribe<TestEvent>(
                _ =>
                {
                });

        subscription.Dispose();
        subscription.Dispose();

        Assert.True(
            true);
    }
}