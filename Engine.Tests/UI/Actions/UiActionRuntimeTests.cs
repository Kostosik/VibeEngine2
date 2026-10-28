using Engine.Core.Events;
using Engine.Core.Math;
using Engine.UI.Actions;
using Engine.UI.Assets;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Tests.UI.Actions;

public sealed class UiActionRuntimeTests
{
    [Fact]
    public void Load_MultipleAssets_UsesSharedActionRegistry()
    {
        using var events = new EventBus();

        using var uiRuntime = new UiActionRuntime(events);

        var receivedCount = 0;

        using var subscription =
            events.Subscribe<StartGameRequestedEvent>(
                _ => receivedCount++);

        uiRuntime.Register(
            "StartGameRequested",
            () => new StartGameRequestedEvent());

        var firstAsset = CreateButtonAsset("FirstMenu");
        var secondAsset = CreateButtonAsset("SecondMenu");

        var firstInstance = uiRuntime.Load(firstAsset.Asset);
        var secondInstance = uiRuntime.Load(secondAsset.Asset);

        var firstButton = Assert.IsType<UiButton>(
            firstInstance.Get(firstAsset.ButtonId));

        var secondButton = Assert.IsType<UiButton>(
            secondInstance.Get(secondAsset.ButtonId));

        firstButton.Click();
        secondButton.Click();

        Assert.Equal(0, receivedCount);
        Assert.Equal(2, events.PendingCount);

        events.DispatchPending();

        Assert.Equal(2, receivedCount);
        Assert.Equal(0, events.PendingCount);
    }

    [Fact]
    public void Dispose_UnregistersBridgeActions()
    {
        using var events = new EventBus();

        var uiRuntime = new UiActionRuntime(events);

        uiRuntime.Register(
            "StartGameRequested",
            () => new StartGameRequestedEvent());

        Assert.True(
            uiRuntime.Actions.Contains("StartGameRequested"));

        uiRuntime.Dispose();

        Assert.False(
            uiRuntime.Actions.Contains("StartGameRequested"));

        var asset = CreateButtonAsset("Menu");

        Assert.Throws<ObjectDisposedException>(
            () => uiRuntime.Load(asset.Asset));
    }

    private static (UiAsset Asset, Guid ButtonId) CreateButtonAsset(
    string name)
    {
        var rootId = Guid.NewGuid();
        var buttonId = Guid.NewGuid();

        var asset = new UiAsset(
            name,
            new Vector2(1280.0f, 720.0f),
            new[]
            {
            new UiAssetElement(
                rootId,
                Guid.Empty,
                UiAssetElementType.Root,
                "Root",
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(1280.0f, 720.0f))),

            new UiAssetElement(
                buttonId,
                rootId,
                UiAssetElementType.Button,
                "StartButton",
                new UiAssetLayout(
                    UiAnchor.Center,
                    Vector2.Zero,
                    new Vector2(240.0f, 60.0f)),
                text: "Start",
                action: "StartGameRequested")
            });

        return (asset, buttonId);
    }

    private sealed record StartGameRequestedEvent : IEvent;
}