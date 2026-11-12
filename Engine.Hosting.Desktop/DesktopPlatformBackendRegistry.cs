using Engine.Platform;

namespace Engine.Hosting.Desktop;

public sealed class DesktopPlatformBackendRegistry
{
    private readonly Dictionary<
        PlatformBackendKind,
        IDesktopPlatformBackend> _backends = new();

    public void Register(IDesktopPlatformBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        if (!_backends.TryAdd(backend.Kind, backend))
        {
            throw new InvalidOperationException(
                $"Platform backend '{backend.Kind}' is already registered.");
        }
    }

    public IDesktopPlatformBackend Get(PlatformBackendKind kind)
    {
        if (!_backends.TryGetValue(kind, out var backend))
        {
            throw new NotSupportedException(
                $"Platform backend '{kind}' is not registered.");
        }

        return backend;
    }
}