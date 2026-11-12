using Engine.Graphics;
using Engine.Graphics.OpenGL;
using Engine.Hosting.Desktop;
using Engine.Platform;

namespace Engine.Hosting.Desktop.SilkNet;

public sealed class OpenGLDesktopGraphicsDeviceFactory
    : IDesktopGraphicsDeviceFactory
{
    private readonly OpenGLGraphicsDeviceFactory _factory = new();

    public IGraphicsDevice CreateDevice(
        IPlatformGraphicsContext context,
        DesktopGraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options.Camera);

        if (context.Kind != GraphicsContextKind.OpenGL)
        {
            throw new NotSupportedException(
                $"Graphics context '{context.Kind}' is not supported.");
        }

        if (context is not IPlatformGraphicsProcedureAddressProvider provider)
        {
            throw new NotSupportedException(
                "OpenGL procedure addresses are unavailable.");
        }

        var graphicsContext = new OpenGLGraphicsDeviceContext(
            provider.GetProcAddress);

        var graphicsOptions = new OpenGLGraphicsDeviceOptions(
            options.LogicalWidth,
            options.LogicalHeight,
            options.FramebufferWidth,
            options.FramebufferHeight,
            options.Camera);

        return _factory.CreateDevice(
            graphicsContext,
            graphicsOptions);
    }
}