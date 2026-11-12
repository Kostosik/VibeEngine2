namespace Engine.Platform;

public interface IPlatformGraphicsContextFactory
{
    IPlatformGraphicsContext Create(
        IPlatformWindow window,
        GraphicsContextKind kind);
}