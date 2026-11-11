using Engine.Camera;

namespace Engine.Graphics.OpenGL;

public readonly record struct OpenGLGraphicsDeviceOptions(
    int LogicalWidth,
    int LogicalHeight,
    int FramebufferWidth,
    int FramebufferHeight,
    Camera2D Camera);