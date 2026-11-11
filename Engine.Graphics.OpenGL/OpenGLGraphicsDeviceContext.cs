using Silk.NET.Core.Contexts;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLGraphicsDeviceContext
    : IOpenGLGraphicsDeviceContext
{
    public IGLContext NativeContext { get; }

    public GraphicsBackendKind Backend =>
        GraphicsBackendKind.OpenGL;

    public OpenGLGraphicsDeviceContext(
        IGLContext nativeContext)
    {
        ArgumentNullException.ThrowIfNull(nativeContext);

        NativeContext = nativeContext;
    }
}