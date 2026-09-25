namespace Engine.Graphics;

public interface IGraphics
{
    IGraphicsDevice Device { get; }

    void Run(Action<double> frame);
}