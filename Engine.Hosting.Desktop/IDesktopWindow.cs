using Engine.Camera;
using Engine.Core.Application;
using Engine.Graphics;
using Engine.Input;
using Engine.Input.Cursors;

namespace Engine.Hosting.Desktop;

public interface IDesktopWindow : IDisposable
{
    int Width { get; }
    int Height { get; }

    bool IsFullscreen { get; set; }

    Camera2D Camera { get; }

    IGraphicsDevice GraphicsDevice { get; }

    IInputBackend InputBackend { get; }

    ITextInput TextInput { get; }

    ICursorService Cursor { get; }

    event Action<int, int>? Resized;

    void Initialize();

    void Run(GameLoop gameLoop);

    void ApplyWindowSettings(
        int width,
        int height,
        bool fullscreen,
        bool vsync);

    void Close();
}