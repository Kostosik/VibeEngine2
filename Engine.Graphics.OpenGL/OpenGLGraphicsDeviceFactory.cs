using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLGraphicsDeviceFactory
    : IGraphicsDeviceFactory<
        IOpenGLGraphicsDeviceContext,
        OpenGLGraphicsDeviceOptions>
{
    public IGraphicsDevice CreateDevice(
        IOpenGLGraphicsDeviceContext context,
        OpenGLGraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options.Camera);

        var gl = GL.GetApi(context.ProcedureAddressResolver);

        return new OpenGLGraphicsDevice(
            gl,
            options.LogicalWidth,
            options.LogicalHeight,
            options.FramebufferWidth,
            options.FramebufferHeight,
            options.Camera);
    }
}