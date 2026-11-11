
using Engine.Input;
using Engine.Input.Cursors;

namespace Engine.Platform;

public interface IPlatformWindow : IDisposable
{
    int Width { get; }

    int Height { get; }

    int FramebufferWidth { get; }

    int FramebufferHeight { get; }

    bool IsFullscreen { get; set; }

    IInputBackend InputBackend { get; }

    ITextInput TextInput { get; }

    IPointerInput? PointerInput { get; }

    ICursorService Cursor { get; }

    event Action? Loaded;

    event Action<double>? Updating;

    event Action<double>? Rendering;

    event Action<int, int>? Resized;

    event Action<int, int>? FramebufferResized;

    event Func<bool>? CloseRequested;

    event Action? Closing;

    void Initialize();

    void Run();

    void CancelCloseRequest();

    void Close();

    void ApplyWindowSettings(
        int width,
        int height,
        bool fullscreen,
        bool vsync);
}
