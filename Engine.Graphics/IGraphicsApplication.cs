namespace Engine.Graphics;

public interface IGraphicsApplication : IDisposable
{
    IGraphicsDevice Graphics { get; }

    void Run();
}