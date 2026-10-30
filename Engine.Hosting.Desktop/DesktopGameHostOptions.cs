using Engine.Content.Loading;
using Engine.Input;
using Engine.Runtime;

namespace Engine.Hosting.Desktop;

public sealed class DesktopGameHostOptions
{
    public string Title { get; init; } = "VibeEngine Game";
    public bool VSync { get; init; } = false;
    public int Width { get; init; } = 1280;

    public int Height { get; init; } = 720;
    public bool Fullscreen { get; init; }
    public string? AssetsDirectory { get; init; }

    public EngineRuntimeOptions RuntimeOptions { get; init; } = new();

    /// <summary>
    /// Optional input map configured by the application.
    /// </summary>
    public InputActionMap? InputActions { get; init; }

    /// <summary>
    /// Adds application-specific content loaders before
    /// the content manager is created.
    /// </summary>
    public Action<ContentLoaderRegistry>? ConfigureContentLoaders
    {
        get;
        init;
    }
}