using Engine.Camera;

namespace Engine.Hosting.Desktop;

public readonly record struct DesktopGraphicsDeviceOptions(
    int LogicalWidth,
    int LogicalHeight,
    int FramebufferWidth,
    int FramebufferHeight,
    Camera2D Camera);