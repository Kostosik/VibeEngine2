using Engine.Core.Events;
using Engine.Graphics.Resources;
using Engine.UI.Assets;

namespace Engine.UI.Actions;

/// <summary>
/// Shared runtime services for loading UI assets and dispatching
/// their named actions to the game's event bus.
/// </summary>
public sealed class UiActionRuntime : IDisposable
{
    private readonly UiActionEventBridge _bridge;
    private readonly ITextureResourceManager? _textures;
    private bool _disposed;

    public UiActionRuntime(
        EventBus events,
        ITextureResourceManager? textures = null)
    {
        ArgumentNullException.ThrowIfNull(events);

        Actions = new UiActionRegistry();
        _textures = textures;

        _bridge = new UiActionEventBridge(
            Actions,
            events);
    }

    public UiActionRegistry Actions { get; }

    public void Register<TEvent>(
        string actionId,
        Func<TEvent> eventFactory)
        where TEvent : IEvent
    {
        EnsureNotDisposed();

        _bridge.Register(
            actionId,
            eventFactory);
    }

    public void Register<TEvent>(
        string actionId,
        Func<UiActionContext, TEvent> eventFactory)
        where TEvent : IEvent
    {
        EnsureNotDisposed();

        _bridge.Register(
            actionId,
            eventFactory);
    }

    public UiAssetInstance Load(UiAsset asset)
    {
        EnsureNotDisposed();

        return new UiAssetLoader(
            _textures,
            Actions).Load(asset);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _bridge.Dispose();
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}