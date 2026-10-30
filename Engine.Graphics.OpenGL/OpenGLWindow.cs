using Engine.Camera;
using Engine.Core.Application;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.Input.SilkNet;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Runtime.ExceptionServices;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLWindow : IDisposable
{
    private bool _startFullscreen;
    private bool _isBorderlessFullscreen;

    private Vector2D<int> _windowedPosition;
    private WindowBorder _windowedBorder;

    private bool _hasFocus = true;
    private readonly IWindow _window;
    private GameLoop? _gameLoop;
    private GL? _gl;
    private OpenGLGraphicsDevice? _graphicsDevice;
    private SilkNetInputBackend? _inputBackend;
    public event Func<bool>? CloseRequested;
    private bool _closeApproved;
    private bool _disposed;
    public ICursorService Cursor =>
    _inputBackend as ICursorService
    ?? throw new InvalidOperationException(
        "Cursor service is not initialized.");

    public int Width => _window.Size.X;

    public int Height => _window.Size.Y;

    public bool IsFullscreen
    {
        get =>
            _window.WindowState ==
            Silk.NET.Windowing.WindowState.Fullscreen;

        set
        {
            var state = value
                ? Silk.NET.Windowing.WindowState.Fullscreen
                : Silk.NET.Windowing.WindowState.Normal;

            if (_window.WindowState != state)
            {
                _window.WindowState = state;
            }
        }
    }

    public ITextInput TextInput =>
    _inputBackend as ITextInput
    ?? throw new InvalidOperationException(
        "Text input is not initialized.");

    public event Action<int, int>? Resized;

    public IGraphicsDevice GraphicsDevice =>
    _graphicsDevice
    ?? throw new InvalidOperationException(
        "Graphics device is not initialized.");

    private bool _initialized;

    public OpenGLWindow(
        int width,
        int height,
        string title,
        bool fullscreen = false,
        bool vsync = false)
    {
        var options =
            WindowOptions.Default;

        options.VSync = vsync;

        options.WindowState =
            WindowState.Normal;

        options.Size =
            new Vector2D<int>(
                width,
                height);

        options.Title =
            title;
        _startFullscreen = fullscreen;
        _window =
            Window.Create(options);

        Camera =
            new Camera2D(
                new Engine.Core.Math.Vector2(
                    width,
                    height));
        _window.FocusChanged += OnFocusChanged;
        _window.Load +=
            OnLoad;

        _window.Resize +=
            OnResize;

        _window.Closing +=
            OnClosing;
    }

    public Camera2D Camera { get; }

    public IInputBackend InputBackend =>
        _inputBackend
        ?? throw new InvalidOperationException(
            "Window has not been initialized.");

    public void Initialize()
    {
        if (_initialized)
        {
            throw new InvalidOperationException(
                "Window has already been initialized.");
        }

        _window.Initialize();

        if (_startFullscreen)
        {
            EnterBorderlessFullscreen();
        }

        _initialized = true;
    }

    public void ApplyWindowSettings(
    int width,
    int height,
    bool fullscreen,
    bool vsync)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _window.VSync = vsync;

        var fullscreenState =
            Silk.NET.Windowing.WindowState.Fullscreen;

        var isCurrentlyFullscreen =
            _window.WindowState == fullscreenState;

        if (fullscreen)
        {
            if (!isCurrentlyFullscreen)
            {
                _window.WindowState = fullscreenState;
            }

            return;
        }

        if (_window.WindowState !=
            Silk.NET.Windowing.WindowState.Normal)
        {
            _window.WindowState =
                Silk.NET.Windowing.WindowState.Normal;
        }

        var currentSize = _window.Size;

        if (currentSize.X != width ||
            currentSize.Y != height)
        {
            _window.Size = new Vector2D<int>(
                width,
                height);
        }
    }

    private void EnterBorderlessFullscreen()
    {
        var monitor = _window.Monitor
            ?? throw new InvalidOperationException(
                "No monitor is available.");

        var bounds = monitor.Bounds;

        _windowedPosition = _window.Position;
        _windowedBorder = _window.WindowBorder;

        _window.WindowBorder = WindowBorder.Hidden;
        _window.Position = bounds.Origin;
        _window.Size = bounds.Size;

        _isBorderlessFullscreen = true;
    }

    private void ExitBorderlessFullscreen(
        int width,
        int height)
    {
        _window.WindowBorder = _windowedBorder;
        _window.Position = _windowedPosition;

        _window.Size = new Vector2D<int>(
            width,
            height);

        _isBorderlessFullscreen = false;
    }

    public void Run(
    GameLoop gameLoop)
    {
        ArgumentNullException.ThrowIfNull(gameLoop);

        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Window must be initialized before Run.");
        }

        _gameLoop = gameLoop;

        _window.Update +=
            _ =>
            {
                gameLoop.Update();
            };

        _window.Render +=
            _ =>
            {
                if (_graphicsDevice is null)
                {
                    return;
                }

                _graphicsDevice.BeginFrame();

                gameLoop.Render();

                _graphicsDevice.EndFrame();
            };

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
                ReleasePlatformResources();
            }
        }
    }

    private void OnLoad()
    {
        _gl =
            GL.GetApi(_window);

        _graphicsDevice =
            new OpenGLGraphicsDevice(
                _gl,
                _window.Size.X,
                _window.Size.Y,
                Camera);

        _inputBackend =
            new SilkNetInputBackend(
                _window.CreateInput());
        _inputBackend.SetFocused(_hasFocus);
    }

    private void OnFocusChanged(bool focused)
    {
        _hasFocus = focused;
        _inputBackend?.SetFocused(focused);
    }
    private void OnResize(
        Vector2D<int> size)
    {
        // При сворачивании окна платформа может
        // сообщить нулевой размер клиентской области.
        // Сохраняем последний допустимый размер.
        if (size.X <= 0 ||
            size.Y <= 0)
        {
            return;
        }

        _graphicsDevice?.Resize(
            size.X,
            size.Y);

        Camera.SetViewportSize(
            new Engine.Core.Math.Vector2(
                size.X,
                size.Y));

        Resized?.Invoke(
            size.X,
            size.Y);
    }

    private bool CanClose()
    {
        var handlers =
            CloseRequested?.GetInvocationList();

        if (handlers is null)
        {
            return true;
        }

        foreach (var handler in handlers)
        {
            var closeHandler =
                (Func<bool>)handler;

            if (!closeHandler())
            {
                return false;
            }
        }

        return true;
    }

    public void Close()
    {
        _closeApproved = true;
        _window.Close();
    }

    private void OnClosing()
    {
        if (!_closeApproved &&
            !CanClose())
        {
            _window.IsClosing = false;
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
            ReleasePlatformResources();
        }
    }

    private void ReleasePlatformResources()
    {
        var errors = new List<Exception>();

        var inputBackend = _inputBackend;
        _inputBackend = null;

        try
        {
            inputBackend?.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        var graphicsDevice = _graphicsDevice;
        _graphicsDevice = null;

        try
        {
            graphicsDevice?.Dispose();
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
                "One or more OpenGL window resources could not be released.",
                errors);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            ReleasePlatformResources();
        }
        finally
        {
            _window.FocusChanged -= OnFocusChanged;
            _window.Dispose();
        }
    }
}