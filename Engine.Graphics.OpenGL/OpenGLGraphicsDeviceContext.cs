namespace Engine.Graphics.OpenGL;

public sealed class OpenGLGraphicsDeviceContext
    : IOpenGLGraphicsDeviceContext
{
    public Func<string, nint> ProcedureAddressResolver { get; }

    public GraphicsBackendKind Backend =>
        GraphicsBackendKind.OpenGL;

    public OpenGLGraphicsDeviceContext(
        Func<string, nint> procedureAddressResolver)
    {
        ArgumentNullException.ThrowIfNull(procedureAddressResolver);

        ProcedureAddressResolver = procedureAddressResolver;
    }
}