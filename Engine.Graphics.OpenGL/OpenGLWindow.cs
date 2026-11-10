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
    private Vector2D<int> _windowedSize;
    private WindowBorder _windowedBorder;

    private bool _hasFocus = true;
    private readonly IWindow _window;
    private GameLoop? _gameLoop;
    private GL? _gl;
    private OpenGLGraphicsDevice? _graphicsDevice;
    private SilkNetInputBackend? _inputBackend;
    public event Func<bool>? CloseRequested;
    private bool _closeApproved;
    private bool _pendingCloseRequest;
    private bool _insideGameLoopCallback;
    private bool _disposed;
    public ICursorService Cursor =>
    _inputBackend as ICursorService
    ?? throw new InvalidOperationException(
        "Cursor service is not initialized.");

    public int Width => _window.Size.X;

    public int Height => _window.Size.Y;

    public int FramebufferWidth =>
    _window.FramebufferSize.X;

    public int FramebufferHeight =>
        _window.FramebufferSize.Y;

    public bool IsFullscreen
    {
        get => _isBorderlessFullscreen;

        set
        {
            // Не выполняем повторное переключение,
            // если состояние уже соответствует запросу.
            if (value == _isBorderlessFullscreen)
            {
                return;
            }

            if (value)
            {
                EnterBorderlessFullscreen();
            }
            else
            {
                ExitBorderlessFullscreen(
                    _windowedSize.X,
                    _windowedSize.Y);
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
        _window.FramebufferResize += OnFramebufferResize;
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

        if (fullscreen)
        {
            if (!_isBorderlessFullscreen)
            {
                EnterBorderlessFullscreen();
            }

            _windowedSize = new Vector2D<int>(
                width,
                height);

            return;
        }

        if (_isBorderlessFullscreen)
        {
            ExitBorderlessFullscreen(width, height);
        }
        else
        {
            _window.Size = new Vector2D<int>(
                width,
                height);
        }
    }

    private void EnterBorderlessFullscreen()
    {
        if (_isBorderlessFullscreen)
            return;

        var monitor = _window.Monitor
            ?? throw new InvalidOperationException(
                "No monitor is available.");

        var bounds = monitor.Bounds;

        _windowedPosition = _window.Position;
        _windowedSize = _window.Size;
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
        // Повторный выход из обычного окна не нужен.
        if (!_isBorderlessFullscreen)
        {
            return;
        }

        // Если размеры не переданы, восстанавливаем
        // последние сохранённые размеры окна.
        if (width <= 0)
        {
            width = _windowedSize.X;
        }

        if (height <= 0)
        {
            height = _windowedSize.Y;
        }

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException(
                "Cannot restore window: saved window size is invalid.");
        }

        // Восстанавливаем оформление окна.
        _window.WindowBorder = _windowedBorder;

        // Восстанавливаем положение.
        _window.Position = _windowedPosition;

        // Восстанавливаем размеры.
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

        _window.Update += _ =>
        {
            if (gameLoop.IsShutdown)
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
        };

        _window.Render += _ =>
        {
            if (_graphicsDevice is null || gameLoop.IsShutdown)
                return;

            _insideGameLoopCallback = true;

            try
            {
                _graphicsDevice.BeginFrame();

                gameLoop.Render();

                _graphicsDevice.EndFrame();
            }
            finally
            {
                _insideGameLoopCallback = false;
            }

            ProcessPendingClose();
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
        _gl = GL.GetApi(_window);

        var windowSize = _window.Size;
        var framebufferSize = _window.FramebufferSize;

        if (windowSize.X <= 0 || windowSize.Y <= 0)
        {
            throw new InvalidOperationException(
                "Window has an invalid initial size.");
        }

        // Некоторые платформы могут ещё не сообщить
        // размер framebuffer на момент загрузки.
        if (framebufferSize.X <= 0 ||
            framebufferSize.Y <= 0)
        {
            framebufferSize = windowSize;
        }

        Camera.SetViewportSize(
            new Engine.Core.Math.Vector2(
                windowSize.X,
                windowSize.Y));

        _graphicsDevice = new OpenGLGraphicsDevice(
            _gl,
            windowSize.X,
            windowSize.Y,
            framebufferSize.X,
            framebufferSize.Y,
            Camera);

        _inputBackend = new SilkNetInputBackend(
            _window.CreateInput());

        _inputBackend.SetFocused(_hasFocus);
    }

    private void OnFocusChanged(bool focused)
    {
        _hasFocus = focused;
        _inputBackend?.SetFocused(focused);
    }
    private void OnResize(Vector2D<int> size)
    {
        // Логические размеры окна.
        if (size.X <= 0 || size.Y <= 0)
        {
            return;
        }

        Camera.SetViewportSize(
            new Engine.Core.Math.Vector2(
                size.X,
                size.Y));

        var framebufferSize = _window.FramebufferSize;

        if (framebufferSize.X > 0 &&
            framebufferSize.Y > 0)
        {
            _graphicsDevice?.Resize(
                size.X,
                size.Y,
                framebufferSize.X,
                framebufferSize.Y);
        }

        // Это событие продолжает передавать
        // именно логические размеры окна.
        Resized?.Invoke(size.X, size.Y);
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        // Событие может прийти с нулевыми размерами,
        // например, во время сворачивания окна.
        if (size.X <= 0 || size.Y <= 0)
        {
            return;
        }

        var windowSize = _window.Size;

        if (windowSize.X <= 0 ||
            windowSize.Y <= 0)
        {
            return;
        }

        _graphicsDevice?.Resize(
            windowSize.X,
            windowSize.Y,
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

        // Во время игрового цикла откладываем закрытие.
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
        if (!_closeApproved &&
            !CanClose())
        {
            _window.IsClosing = false;
            return;
        }
        // Даже системный запрос закрытия не должен
        // останавливать игровой цикл посреди его выполнения.
        if (_insideGameLoopCallback)
        {
            _window.IsClosing = false;
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
            _window.Resize -= OnResize;
            _window.FramebufferResize -= OnFramebufferResize;
            _window.Load -= OnLoad;
            _window.Closing -= OnClosing;
            _window.Dispose();
        }
    }
}