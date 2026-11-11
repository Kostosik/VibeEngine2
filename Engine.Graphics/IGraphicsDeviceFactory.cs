namespace Engine.Graphics;

public interface IGraphicsDeviceFactory<TContext, TOptions>
{
    IGraphicsDevice CreateDevice(
        TContext context,
        TOptions options);
}