using Engine.Graphics;
using Engine.Platform;

namespace Engine.Hosting.Desktop;

public interface IDesktopGraphicsBackend
{
    GraphicsBackendKind Kind { get; }

    GraphicsContextKind ContextKind { get; }

    IDesktopGraphicsDeviceFactory DeviceFactory { get; }
}