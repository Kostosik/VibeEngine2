using Engine.Graphics;
using Engine.Platform;

namespace Engine.Hosting.Desktop;

public interface IDesktopGraphicsDeviceFactory
{
    IGraphicsDevice CreateDevice(
        IPlatformGraphicsContext context,
        DesktopGraphicsDeviceOptions options);
}