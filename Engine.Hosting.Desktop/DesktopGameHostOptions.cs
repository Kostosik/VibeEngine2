using Engine.Runtime;

namespace Engine.Hosting.Desktop;

public sealed class DesktopGameHostOptions
{
    public string Title { get; init; } = "VibeEngine Game";

    public int Width { get; init; } = 1280;

    public int Height { get; init; } = 720;

    public string? AssetsDirectory { get; init; }

    public EngineRuntimeOptions RuntimeOptions { get; init; } = new();
}