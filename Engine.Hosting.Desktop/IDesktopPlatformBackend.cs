using Engine.Platform;

namespace Engine.Hosting.Desktop;

public interface IDesktopPlatformBackend
{
    PlatformBackendKind Kind { get; }

    IReadOnlyCollection<GraphicsContextKind> SupportedGraphicsContexts { get; }

    IPlatformWindowFactory<IPlatformWindow> WindowFactory { get; }

    IPlatformGraphicsContextFactory GraphicsContextFactory { get; }
}