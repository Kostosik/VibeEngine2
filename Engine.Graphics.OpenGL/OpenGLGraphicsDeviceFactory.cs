using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLGraphicsDeviceFactory
    : IGraphicsDeviceFactory<GL, OpenGLGraphicsDeviceOptions>
{
    public IGraphicsDevice CreateDevice(
        GL context,
        OpenGLGraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options.Camera);

        return new OpenGLGraphicsDevice(
            context,
            options.LogicalWidth,
            options.LogicalHeight,
            options.FramebufferWidth,
            options.FramebufferHeight,
            options.Camera);
    }
}