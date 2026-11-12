using Engine.Graphics;

namespace Engine.Hosting.Desktop;

public sealed class DesktopGraphicsBackendRegistry
{
    private readonly Dictionary<
        GraphicsBackendKind,
        IDesktopGraphicsBackend> _backends = new();

    public void Register(IDesktopGraphicsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        if (!_backends.TryAdd(backend.Kind, backend))
        {
            throw new InvalidOperationException(
                $"Graphics backend '{backend.Kind}' is already registered.");
        }
    }

    public IDesktopGraphicsBackend Get(GraphicsBackendKind kind)
    {
        if (!_backends.TryGetValue(kind, out var backend))
        {
            throw new NotSupportedException(
                $"Graphics backend '{kind}' is not registered.");
        }

        return backend;
    }
}