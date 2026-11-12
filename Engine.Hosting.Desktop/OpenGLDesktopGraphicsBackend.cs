using Engine.Graphics;
using Engine.Platform;

namespace Engine.Hosting.Desktop;

public sealed class OpenGLDesktopGraphicsBackend
    : IDesktopGraphicsBackend
{
    private readonly IDesktopGraphicsDeviceFactory _deviceFactory =
        new OpenGLDesktopGraphicsDeviceFactory();

    public GraphicsBackendKind Kind =>
        GraphicsBackendKind.OpenGL;

    public GraphicsContextKind ContextKind =>
        GraphicsContextKind.OpenGL;

    public IDesktopGraphicsDeviceFactory DeviceFactory =>
        _deviceFactory;
}