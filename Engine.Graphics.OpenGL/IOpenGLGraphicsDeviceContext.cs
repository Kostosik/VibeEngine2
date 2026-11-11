using Silk.NET.Core.Contexts;

namespace Engine.Graphics.OpenGL;

public interface IOpenGLGraphicsDeviceContext
    : IGraphicsDeviceContext
{
    IGLContext NativeContext { get; }
}