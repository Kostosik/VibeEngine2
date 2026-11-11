
namespace Engine.Platform;

public sealed record PlatformWindowOptions
{
    public string Title { get; init; } = "VibeEngine Game";

    public int Width { get; init; } = 1280;

    public int Height { get; init; } = 720;

    public bool Fullscreen { get; init; }

    public bool VSync { get; init; }

    public GraphicsContextKind GraphicsContext { get; init; }
        = GraphicsContextKind.OpenGL;
}
