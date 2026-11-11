
using Engine.Input;
using Engine.Input.Cursors;
using Engine.Input.SilkNet;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace Engine.Platform.SilkNet;

public sealed class SilkNetWindow : ISilkNetWindow
{
    private readonly IWindow _window;
    private readonly PlatformWindowOptions _options;

    private SilkNetInputBackend? _inputBackend;

    private Vector2D<int> _windowedPosition;
    private Vector2D<int> _windowedSize;

    private bool _startFullscreen;
    private bool _isFullscreen;
    private bool _hasFocus = true;
    private bool _initialized;
    private bool _disposed;

    public IWindow NativeWindow => _window;

    public int Width => _window.Size.X;

    public int Height => _window.Size.Y;

    public int FramebufferWidth => _window.FramebufferSize.X;

    public int FramebufferHeight => _window.FramebufferSize.Y;

    public bool IsFullscreen
    {
        get => _isFullscreen;
        set
        {
            if (value == _isFullscreen)
                return;

            if (value)
                EnterFullscreen();
            else
                ExitFullscreen(
                    _windowedSize.X,
                    _windowedSize.Y);
        }
    }

    public IInputBackend InputBackend =>
        _inputBackend
        ?? throw new InvalidOperationException(
            "Window has not been initialized.");

    public ITextInput TextInput =>
        _inputBackend as ITextInput
        ?? throw new InvalidOperationException(
            "Text input is not initialized.");

    public IPointerInput? PointerInput =>
        _inputBackend as IPointerInput;

    public ICursorService Cursor =>
        _inputBackend as ICursorService
        ?? throw new InvalidOperationException(
            "Cursor service is not initialized.");

    public event Action? Loaded;
    public event Action<double>? Updating;
    public event Action<double>? Rendering;
    public event Action<int, int>? Resized;
    public event Action<int, int>? FramebufferResized;
    public event Func<bool>? CloseRequested;
    public event Action? Closing;

    public SilkNetWindow(PlatformWindowOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Width));

        if (options.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Height));

        ArgumentException.ThrowIfNullOrWhiteSpace(options.Title);

        _options = options;
        _startFullscreen = options.Fullscreen;

        var windowOptions = WindowOptions.Default;

        windowOptions.VSync = options.VSync;
        windowOptions.WindowState = WindowState.Normal;

        windowOptions.Size = new Vector2D<int>(
            options.Width,
            options.Height);

        windowOptions.Title = options.Title;

        windowOptions.API = options.GraphicsContext switch
        {
            GraphicsContextKind.OpenGL => windowOptions.API,
            GraphicsContextKind.None => GraphicsAPI.None,
            _ => throw new ArgumentOutOfRangeException(
                nameof(options.GraphicsContext))
        };

        GlfwWindowing.Use();

        _window = Window.Create(windowOptions);

        _window.FocusChanged += OnFocusChanged;
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Resize += OnResize;
        _window.FramebufferResize += OnFramebufferResize;
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

        if (_startFullscreen)
            EnterFullscreen();

        _initialized = true;
    }

    public void Run()
    {
        ThrowIfDisposed();

        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Window must be initialized before Run.");
        }

        // Используем перегрузку с callback,
        // чтобы Silk.NET самостоятельно не вызывал Reset().
        _window.Run(() =>
        {
            _window.DoEvents();

            if (!_window.IsClosing)
            {
                _window.DoUpdate();
            }

            if (!_window.IsClosing)
            {
                _window.DoRender();
            }
        });

        // Обрабатываем оставшиеся события после выхода из цикла.
        _window.DoEvents();
    }

    public void ApplyWindowSettings(
        int width,
        int height,
        bool fullscreen,
        bool vsync)
    {
        ThrowIfDisposed();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _window.VSync = vsync;

        if (fullscreen)
        {
            if (!_isFullscreen)
                EnterFullscreen();

            _windowedSize = new Vector2D<int>(width, height);
            return;
        }

        if (_isFullscreen)
        {
            ExitFullscreen(width, height);
        }
        else
        {
            _window.Size = new Vector2D<int>(width, height);
            _windowedSize = new Vector2D<int>(width, height);
        }
    }

    private void EnterFullscreen()
    {
        if (_isFullscreen)
            return;

        var currentSize = _window.Size;

        if (currentSize.X <= 0 || currentSize.Y <= 0)
            currentSize = _windowedSize;

        if (currentSize.X <= 0 || currentSize.Y <= 0)
        {
            throw new InvalidOperationException(
                "Cannot enter fullscreen: window size is invalid.");
        }

        _windowedPosition = _window.Position;
        _windowedSize = currentSize;

        _window.WindowState = WindowState.Fullscreen;

        _isFullscreen = true;
    }

    private void ExitFullscreen(int width, int height)
    {
        if (!_isFullscreen)
            return;

        if (width <= 0)
            width = _windowedSize.X;

        if (height <= 0)
            height = _windowedSize.Y;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException(
                "Cannot exit fullscreen: saved window size is invalid.");
        }

        _window.WindowState = WindowState.Normal;

        _window.Size = new Vector2D<int>(width, height);
        _window.Position = _windowedPosition;

        _windowedSize = new Vector2D<int>(width, height);
        _isFullscreen = false;
    }

    private void OnLoad()
    {
        _inputBackend = new SilkNetInputBackend(
            _window.CreateInput());

        _inputBackend.SetFocused(_hasFocus);

        Loaded?.Invoke();
    }

    private void OnUpdate(double deltaTime)
    {
        Updating?.Invoke(deltaTime);
    }

    private void OnRender(double deltaTime)
    {
        Rendering?.Invoke(deltaTime);
    }

    private void OnFocusChanged(bool focused)
    {
        _hasFocus = focused;
        _inputBackend?.SetFocused(focused);
    }

    private void OnResize(Vector2D<int> size)
    {
        if (size.X <= 0 || size.Y <= 0)
            return;

        Resized?.Invoke(size.X, size.Y);
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        if (size.X <= 0 || size.Y <= 0)
            return;

        FramebufferResized?.Invoke(size.X, size.Y);
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

    private void OnClosing()
    {
        if (!CanClose())
        {
            _window.IsClosing = false;
            return;
        }

        Closing?.Invoke();
    }

    public void Close()
    {
        ThrowIfDisposed();
        _window.Close();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void DisposeInputBackend()
    {
        var inputBackend = _inputBackend;

        if (inputBackend is null)
            return;

        _inputBackend = null;
        inputBackend.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        var errors = new List<Exception>();

        _window.FocusChanged -= OnFocusChanged;
        _window.Load -= OnLoad;
        _window.Update -= OnUpdate;
        _window.Render -= OnRender;
        _window.Resize -= OnResize;
        _window.FramebufferResize -= OnFramebufferResize;
        _window.Closing -= OnClosing;

        try
        {
            DisposeInputBackend();
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

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more platform resources could not be released.",
                errors);
        }
    }
}
