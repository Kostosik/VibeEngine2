using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Graphics.OpenGL;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.Runtime;

namespace Engine.Hosting.Desktop;

public static class DesktopGameHost
{
    public static void Run<TGame>(
        Func<EngineRuntime, TGame> createGame,
        DesktopGameHostOptions? options = null)
        where TGame : EngineGame
    {
        ArgumentNullException.ThrowIfNull(createGame);

        options ??= new DesktopGameHostOptions();

        ArgumentException.ThrowIfNullOrWhiteSpace(options.Title);

        if (options.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.Width));
        }

        if (options.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.Height));
        }

        ArgumentNullException.ThrowIfNull(options.RuntimeOptions);

        var assetsDirectory = Path.GetFullPath(
            options.AssetsDirectory
            ?? Path.Combine(
                AppContext.BaseDirectory,
                "Assets"));

        Directory.CreateDirectory(assetsDirectory);

        using var window = new OpenGLWindow(
            options.Width,
            options.Height,
            options.Title);

        window.Initialize();

        var assetSource = new FileAssetSource(
            assetsDirectory);

        var contentCatalog = new FileContentCatalog(
            assetsDirectory);

        var contentLoaders = new ContentLoaderRegistry();

        contentLoaders.Register(
            new ImageTextureContentLoader());

        using var content = new ContentManager(
            assetSource,
            contentCatalog,
            contentLoaders);

        using var audio = new AudioManager(
            assetSource,
            new OpenALAudioDevice());

        var inputActions = new InputActionMap();

        var input = new ActionInput(
            window.InputBackend,
            inputActions);

        var runtimeServices = new EngineRuntimeServices(
            window.GraphicsDevice,
            input,
            window.Camera,
            audio,
            content,
            inputActions);

        using var runtime = new EngineRuntime(
            options.RuntimeOptions,
            runtimeServices);

        var game = createGame(runtime)
            ?? throw new InvalidOperationException(
                "The game factory returned null.");

        var gameLoop = new GameLoop(
            game,
            runtime.SimulationRate);

        if (runtime.DebugRegistry is not null)
        {
            runtime.AttachGameLoopController(gameLoop);
        }

        try
        {
            gameLoop.Initialize();
            window.Run(gameLoop);
        }
        finally
        {
            if (gameLoop.IsInitialized &&
                !gameLoop.IsShutdown)
            {
                gameLoop.Shutdown();
            }
        }
    }
}