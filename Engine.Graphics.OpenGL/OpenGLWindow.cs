using Engine.Core.Application;
using Engine.Camera;
using Engine.Input;
using Engine.Input.Cursors;
using Engine.Input.SilkNet;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLWindow : IDisposable
{
    private readonly IWindow _window;
    private GameLoop? _gameLoop;
    private GL? _gl;
    private OpenGLGraphicsDevice? _graphicsDevice;
    private SilkNetInputBackend? _inputBackend;
    public event Func<bool>? CloseRequested;
    private bool _closeApproved;

    public ICursorService Cursor =>
    _inputBackend as ICursorService
    ?? throw new InvalidOperationException(
        "Cursor service is not initialized.");

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
        string title)
    {
        var options =
            WindowOptions.Default;
        
        options.VSync = false;



        options.Size =
            new Vector2D<int>(
                width,
                height);

        options.Title =
            title;

        _window =
            Window.Create(options);

        Camera =
            new Camera2D(
                new Engine.Core.Math.Vector2(
                    width,
                    height));

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

        _initialized = true;
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
            if (!gameLoop.IsShutdown)
            {
                gameLoop.Shutdown();
            }

            _graphicsDevice?.Dispose();
            _graphicsDevice = null;
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
    }

    private void OnResize(
        Vector2D<int> size)
    {
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
            _graphicsDevice?.Dispose();
            _graphicsDevice = null;
        }
    }

    public void Dispose()
    {
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;

        _window.Dispose();
    }
}