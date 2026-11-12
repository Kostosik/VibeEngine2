using Engine.Core.Application;
using Engine.Hosting.Desktop;
using Engine.Runtime;

namespace Engine.Hosting.Desktop.SilkNet;

public static class SilkNetDesktopHost
{
    public static void Run(
        Func<DesktopGameHostContext, IApplication> createApplication,
        DesktopGameHostOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(createApplication);

        DesktopGameHost.Run(
            createApplication,
            ConfigureDefaults(options));
    }

    public static void Run<TGame>(
        Func<EngineRuntime, TGame> createGame,
        DesktopGameHostOptions? options = null)
        where TGame : EngineGame
    {
        ArgumentNullException.ThrowIfNull(createGame);

        DesktopGameHost.Run(
            createGame,
            ConfigureDefaults(options));
    }

    private static DesktopGameHostOptions ConfigureDefaults(
        DesktopGameHostOptions? options)
    {
        options ??= new DesktopGameHostOptions();

        return new DesktopGameHostOptions
        {
            GraphicsBackend = options.GraphicsBackend,
            PlatformBackend = options.PlatformBackend,
            Title = options.Title,
            VSync = options.VSync,
            Width = options.Width,
            Height = options.Height,
            Fullscreen = options.Fullscreen,
            AssetsDirectory = options.AssetsDirectory,
            RuntimeOptions = options.RuntimeOptions,
            InputActions = options.InputActions,
            ConfigureContentLoaders = options.ConfigureContentLoaders,

            ConfigureGraphicsBackends = registry =>
            {
                registry.Register(
                    new OpenGLDesktopGraphicsBackend());

                options.ConfigureGraphicsBackends?.Invoke(registry);
            },

            ConfigurePlatformBackends = registry =>
            {
                registry.Register(
                    new SilkNetDesktopPlatformBackend());

                options.ConfigurePlatformBackends?.Invoke(registry);
            }
        };
    }
}