using Engine.Platform;
using Silk.NET.Core.Contexts;

namespace Engine.Platform.SilkNet;

public sealed class SilkNetOpenGLGraphicsContext
    : IPlatformGraphicsProcedureAddressProvider
{
    private readonly IGLContext _context;

    public GraphicsContextKind Kind =>
        GraphicsContextKind.OpenGL;

    public SilkNetOpenGLGraphicsContext(
        IGLContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    public nint GetProcAddress(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _context.GetProcAddress(name);
    }
}