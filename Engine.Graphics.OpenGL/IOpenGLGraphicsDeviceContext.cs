namespace Engine.Graphics.OpenGL;

public interface IOpenGLGraphicsDeviceContext
    : IGraphicsDeviceContext
{
    Func<string, nint> ProcedureAddressResolver { get; }
}