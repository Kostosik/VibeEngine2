namespace Engine.Graphics;

public interface IGraphicsDeviceFactory<TContext, TOptions>
    where TContext : IGraphicsDeviceContext
{
    IGraphicsDevice CreateDevice(
        TContext context,
        TOptions options);
}