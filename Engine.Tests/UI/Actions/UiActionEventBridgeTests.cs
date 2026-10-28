using Engine.Core.Events;
using Engine.UI.Actions;
using Engine.UI.Assets;

namespace Engine.Tests.UI.Actions;

public sealed class UiActionEventBridgeTests
{
    [Fact]
    public void Register_PublishesTypedEventThroughEventBus()
    {
        using var events = new EventBus();

        var actions = new UiActionRegistry();

        using var bridge = new UiActionEventBridge(
            actions,
            events);

        var received = new List<string>();

        using var subscription =
            events.Subscribe<StartGameRequestedEvent>(
                @event => received.Add(@event.ElementName));

        bridge.Register(
            "StartGameRequested",
            context => new StartGameRequestedEvent(
                context.ElementName));

        var context = new UiActionContext(
            Guid.NewGuid(),
            "StartButton",
            UiAssetElementType.Button,
            UiActionTrigger.Click,
            null);

        Assert.True(
            actions.Invoke(
                "StartGameRequested",
                context));

        Assert.Empty(received);
        Assert.Equal(1, events.PendingCount);

        events.DispatchPending();

        Assert.Equal(
            new[] { "StartButton" },
            received);
    }

    [Fact]
    public void Dispose_UnregistersRegisteredActions()
    {
        var actions = new UiActionRegistry();

        using var events = new EventBus();

        var bridge = new UiActionEventBridge(
            actions,
            events);

        bridge.Register(
            "StartGameRequested",
            () => new StartGameRequestedEvent(""));

        Assert.True(
            actions.Contains("StartGameRequested"));

        bridge.Dispose();

        Assert.False(
            actions.Contains("StartGameRequested"));

        bridge.Dispose();
    }

    private sealed record StartGameRequestedEvent(
        string ElementName) : IEvent;
}