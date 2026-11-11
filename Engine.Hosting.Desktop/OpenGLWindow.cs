
using Engine.Camera;
using Engine.Core.Application;
using Engine.Graphics;
using Engine.Graphics.OpenGL;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.Platform;
using Engine.Platform.SilkNet;
using System.Runtime.ExceptionServices;

namespace Engine.Hosting.Desktop;

public sealed class OpenGLWindow : IDesktopWindow
{
    private readonly IPlatformWindow _window;

    private readonly Func<IOpenGLGraphicsDeviceContext> _graphicsContextFactory;

    private readonly IGraphicsDeviceFactory<
        IOpenGLGraphicsDeviceContext,
        OpenGLGraphicsDeviceOptions> _graphicsDeviceFactory;

    private GameLoop? _gameLoop;
    private IGraphicsDevice? _graphicsDevice;

    private int _graphicsWindowWidth;
    private int _graphicsWindowHeight;
    private int _graphicsFramebufferWidth;
    private int _graphicsFramebufferHeight;

    private bool _graphicsResizePending;
    private bool _insideGameLoopCallback;
    private bool _pendingCloseRequest;
    private bool _closeApproved;
    private bool _initialized;
    private bool _disposed;

    public event Func<bool>? CloseRequested;

    public int Width => _window.Width;
    public int Height => _window.Height;

    public int FramebufferWidth =>
        _window.FramebufferWidth;

    public int FramebufferHeight =>
        _window.FramebufferHeight;

    public bool IsFullscreen
    {
        get => _window.IsFullscreen;
        set => _window.IsFullscreen = value;
    }

    public Camera2D Camera { get; }

    public IInputBackend InputBackend =>
        _window.InputBackend;

    public ITextInput TextInput =>
        _window.TextInput;

    public ICursorService Cursor =>
        _window.Cursor;

    public IGraphicsDevice GraphicsDevice =>
        _graphicsDevice
        ?? throw new InvalidOperationException(
            "Graphics device is not initialized.");

    public event Action<int, int>? Resized;

    public OpenGLWindow(
    IPlatformWindow platformWindow,
    Func<IOpenGLGraphicsDeviceContext> graphicsContextFactory,
    IGraphicsDeviceFactory<
        IOpenGLGraphicsDeviceContext,
        OpenGLGraphicsDeviceOptions> graphicsDeviceFactory)
    {
        ArgumentNullException.ThrowIfNull(platformWindow);
        ArgumentNullException.ThrowIfNull(graphicsContextFactory);
        ArgumentNullException.ThrowIfNull(graphicsDeviceFactory);

        _window = platformWindow;
        _graphicsContextFactory = graphicsContextFactory;
        _graphicsDeviceFactory = graphicsDeviceFactory;

        Camera = new Camera2D(
            new Engine.Core.Math.Vector2(
                _window.Width,
                _window.Height));

        _window.Loaded += OnLoad;
        _window.Updating += OnUpdate;
        _window.Rendering += OnRender;
        _window.Resized += OnResize;
        _window.FramebufferResized += OnFramebufferResize;
        _window.Closing += OnClosing;
    }

    public void Initialize()
    {
        ThrowIfDisposed();

        if (_initialized)
        {
            throw new InvalidOperationException(
                "Window has already been initialized.");
        }

        _window.Initialize();
        _initialized = true;
    }

    public void ApplyWindowSettings(
        int width,
        int height,
        bool fullscreen,
        bool vsync)
    {
        ThrowIfDisposed();

        _window.ApplyWindowSettings(
            width,
            height,
            fullscreen,
            vsync);
    }

    public void Run(GameLoop gameLoop)
    {
        ArgumentNullException.ThrowIfNull(gameLoop);
        ThrowIfDisposed();

        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Window must be initialized before Run.");
        }

        if (_gameLoop is not null)
        {
            throw new InvalidOperationException(
                "Window is already running a game loop.");
        }

        _gameLoop = gameLoop;

        try
        {
            _window.Run();
        }
        finally
        {
            try
            {
                if (!gameLoop.IsShutdown)
                {
                    gameLoop.Shutdown();
                }
            }
            finally
            {
                ReleaseGraphicsDevice();
            }
        }
    }

    private void OnLoad()
    {
        var windowWidth = _window.Width;
        var windowHeight = _window.Height;

        var framebufferWidth = _window.FramebufferWidth;
        var framebufferHeight = _window.FramebufferHeight;

        if (windowWidth <= 0 || windowHeight <= 0)
        {
            throw new InvalidOperationException(
                "Window has an invalid initial size.");
        }

        if (framebufferWidth <= 0 || framebufferHeight <= 0)
        {
            framebufferWidth = windowWidth;
            framebufferHeight = windowHeight;
        }

        Camera.SetViewportSize(
            new Engine.Core.Math.Vector2(
                windowWidth,
                windowHeight));

        var graphicsContext = _graphicsContextFactory()
            ?? throw new InvalidOperationException(
                "Graphics context factory returned null.");

        _graphicsDevice = _graphicsDeviceFactory.CreateDevice(
            graphicsContext,
            new OpenGLGraphicsDeviceOptions(
                windowWidth,
                windowHeight,
                framebufferWidth,
                framebufferHeight,
                Camera));

        _graphicsWindowWidth = windowWidth;
        _graphicsWindowHeight = windowHeight;

        _graphicsFramebufferWidth = framebufferWidth;
        _graphicsFramebufferHeight = framebufferHeight;

        _graphicsResizePending = false;
    }

    private void OnUpdate(double deltaTime)
    {
        var gameLoop = _gameLoop;

        if (gameLoop is null || gameLoop.IsShutdown)
            return;

        _insideGameLoopCallback = true;

        try
        {
            gameLoop.Update();
        }
        finally
        {
            _insideGameLoopCallback = false;
        }

        ProcessPendingClose();
    }

    private void OnRender(double deltaTime)
    {
        var graphicsDevice = _graphicsDevice;
        var gameLoop = _gameLoop;

        if (graphicsDevice is null ||
            gameLoop is null ||
            gameLoop.IsShutdown)
        {
            return;
        }

        _insideGameLoopCallback = true;

        try
        {
            ApplyPendingGraphicsResize();

            graphicsDevice.BeginFrame();

            gameLoop.Render();

            graphicsDevice.EndFrame();
        }
        finally
        {
            _insideGameLoopCallback = false;
        }

        ProcessPendingClose();
    }

    private void OnResize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        Camera.SetViewportSize(
            new Engine.Core.Math.Vector2(width, height));

        _graphicsResizePending = true;

        Resized?.Invoke(width, height);
    }

    private void OnFramebufferResize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _graphicsResizePending = true;
    }

    private void ApplyPendingGraphicsResize()
    {
        var graphicsDevice = _graphicsDevice;

        if (!_graphicsResizePending ||
            graphicsDevice is null)
        {
            return;
        }

        var windowWidth = _window.Width;
        var windowHeight = _window.Height;

        var framebufferWidth = _window.FramebufferWidth;
        var framebufferHeight = _window.FramebufferHeight;

        if (windowWidth <= 0 ||
            windowHeight <= 0 ||
            framebufferWidth <= 0 ||
            framebufferHeight <= 0)
        {
            return;
        }

        var unchanged =
            _graphicsWindowWidth == windowWidth &&
            _graphicsWindowHeight == windowHeight &&
            _graphicsFramebufferWidth == framebufferWidth &&
            _graphicsFramebufferHeight == framebufferHeight;

        if (unchanged)
        {
            _graphicsResizePending = false;
            return;
        }

        graphicsDevice.Resize(
            windowWidth,
            windowHeight,
            framebufferWidth,
            framebufferHeight);

        _graphicsWindowWidth = windowWidth;
        _graphicsWindowHeight = windowHeight;

        _graphicsFramebufferWidth = framebufferWidth;
        _graphicsFramebufferHeight = framebufferHeight;

        _graphicsResizePending = false;
    }

    private bool CanClose()
    {
        var handlers = CloseRequested?.GetInvocationList();

        if (handlers is null)
            return true;

        foreach (var handler in handlers)
        {
            var closeHandler = (Func<bool>)handler;

            if (!closeHandler())
                return false;
        }

        return true;
    }

    private void ProcessPendingClose()
    {
        if (!_pendingCloseRequest || _disposed)
            return;

        _pendingCloseRequest = false;
        _closeApproved = true;

        _window.Close();
    }

    public void Close()
    {
        if (_disposed)
            return;

        if (_insideGameLoopCallback)
        {
            _pendingCloseRequest = true;
            _gameLoop?.RequestShutdown();
            return;
        }

        _closeApproved = true;
        _window.Close();
    }

    private void OnClosing()
    {
        if (!_closeApproved && !CanClose())
        {
            _window.CancelCloseRequest();
            return;
        }

        if (_insideGameLoopCallback)
        {
            _window.CancelCloseRequest();

            _pendingCloseRequest = true;
            _gameLoop?.RequestShutdown();

            return;
        }

        _closeApproved = false;

        try
        {
            if (_gameLoop is not null &&
                !_gameLoop.IsShutdown)
            {
                _gameLoop.Shutdown();
            }
        }
        finally
        {
            ReleaseGraphicsDevice();
        }
    }

    private void ReleaseGraphicsDevice()
    {
        var graphicsDevice = _graphicsDevice;
        _graphicsDevice = null;

        graphicsDevice?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _window.Loaded -= OnLoad;
        _window.Updating -= OnUpdate;
        _window.Rendering -= OnRender;
        _window.Resized -= OnResize;
        _window.FramebufferResized -= OnFramebufferResize;
        _window.Closing -= OnClosing;

        var errors = new List<Exception>();

        try
        {
            ReleaseGraphicsDevice();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        try
        {
            _window.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        if (errors.Count == 1)
        {
            ExceptionDispatchInfo
                .Capture(errors[0])
                .Throw();
        }

        if (errors.Count > 1)
        {
            throw new AggregateException(
                "One or more window resources could not be released.",
                errors);
        }
    }
}
