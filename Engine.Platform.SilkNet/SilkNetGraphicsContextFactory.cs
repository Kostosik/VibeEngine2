using Engine.Platform;

namespace Engine.Platform.SilkNet;

public sealed class SilkNetGraphicsContextFactory
    : IPlatformGraphicsContextFactory
{
    public IPlatformGraphicsContext Create(
        IPlatformWindow window,
        GraphicsContextKind kind)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window is not ISilkNetWindow silkWindow)
        {
            throw new ArgumentException(
                "Window is not backed by Silk.NET.",
                nameof(window));
        }

        return kind switch
        {
            GraphicsContextKind.OpenGL =>
                new SilkNetOpenGLGraphicsContext(
                    silkWindow.OpenGLContext),

            _ => throw new NotSupportedException(
                $"Graphics context '{kind}' is not supported.")
        };
    }
}