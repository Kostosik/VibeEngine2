using Engine.Audio;
using Engine.Camera;
using Engine.Content;
using Engine.Core.Assets;
using Engine.Graphics;
using Engine.Graphics.OpenGL;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.Runtime;

namespace Engine.Hosting.Desktop;

/// <summary>
/// Services supplied by the desktop host to an application.
/// Platform-specific implementations remain inside the host.
/// </summary>
public sealed class DesktopGameHostContext : IDisposable
{
    private readonly OpenGLWindow _window;
    private bool _disposed;

    internal DesktopGameHostContext(
        EngineRuntime runtime,
        IAssetSource assetSource,
        IContentManager content,
        InputActionMap inputActions,
        OpenGLWindow window)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(assetSource);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(inputActions);
        ArgumentNullException.ThrowIfNull(window);

        Runtime = runtime;
        AssetSource = assetSource;
        Content = content;
        InputActions = inputActions;
        _window = window;

        _window.Resized += OnWindowResized;
    }

    public EngineRuntime Runtime { get; }

    public IAssetSource AssetSource { get; }

    public IContentManager Content { get; }

    public InputActionMap InputActions { get; }
    public int WindowWidth => _window.Width;

    public int WindowHeight => _window.Height;
    public IGraphicsDevice Graphics =>
        Runtime.Services.Graphics;

    public bool IsFullscreen
    {
        get => _window.IsFullscreen;
        set => _window.IsFullscreen = value;
    }

    public IInput Input =>
        Runtime.Services.Input;

    public IAudioManager Audio =>
        Runtime.Services.Audio;

    public Camera2D Camera =>
        Runtime.Services.Camera;

    public void ApplyWindowSettings(
    int width,
    int height,
    bool fullscreen,
    bool vsync)
    {
        _window.ApplyWindowSettings(
            width,
            height,
            fullscreen,
            vsync);
    }

    /// <summary>
    /// Optional capability: a host may not provide pointer input.
    /// </summary>
    public IPointerInput? PointerInput =>
        _window.InputBackend as IPointerInput;

    public ITextInput TextInput =>
        _window.TextInput;

    public ICursorService Cursor =>
        _window.Cursor;

    public event Action<int, int>? Resized;

    private void OnWindowResized(
        int width,
        int height)
    {
        Resized?.Invoke(width, height);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _window.Resized -= OnWindowResized;
        Resized = null;
        _disposed = true;
    }
}