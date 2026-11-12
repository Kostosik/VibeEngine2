using Engine.Hosting.Desktop;
using Engine.Platform;
using Engine.Platform.SilkNet;

namespace Engine.Hosting.Desktop.SilkNet;

public sealed class SilkNetDesktopPlatformBackend
    : IDesktopPlatformBackend
{
    private readonly IPlatformWindowFactory<IPlatformWindow>
        _windowFactory = new SilkNetWindowFactory();

    private readonly IPlatformGraphicsContextFactory
        _graphicsContextFactory = new SilkNetGraphicsContextFactory();

    private static readonly IReadOnlyCollection<GraphicsContextKind>
        SupportedContexts = Array.AsReadOnly(
        new[]
        {
            GraphicsContextKind.None,
            GraphicsContextKind.OpenGL
        });

    public PlatformBackendKind Kind =>
        PlatformBackendKind.SilkNet;

    public IReadOnlyCollection<GraphicsContextKind>
        SupportedGraphicsContexts => SupportedContexts;

    public IPlatformWindowFactory<IPlatformWindow>
        WindowFactory => _windowFactory;

    public IPlatformGraphicsContextFactory
        GraphicsContextFactory => _graphicsContextFactory;
}