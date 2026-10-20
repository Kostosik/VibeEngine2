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
    /// <summary>
    /// Runs an application using the standard desktop services.
    /// The application controls its own lifecycle and runtime usage.
    /// </summary>
    public static void Run(
        Func<DesktopGameHostContext, IApplication> createApplication,
        DesktopGameHostOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(createApplication);

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

        options.ConfigureContentLoaders?.Invoke(
            contentLoaders);

        using var content = new ContentManager(
            assetSource,
            contentCatalog,
            contentLoaders);

        using var audio = new AudioManager(
            assetSource,
            new OpenALAudioDevice());

        var inputActions =
            options.InputActions ?? new InputActionMap();

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

        using var context = new DesktopGameHostContext(
            runtime,
            assetSource,
            content,
            inputActions,
            window);

        var application = createApplication(context)
            ?? throw new InvalidOperationException(
                "The application factory returned null.");

        var hostedApplication = new HostedApplication(application);

        var gameLoop = new GameLoop(
            hostedApplication,
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
            try
            {
                if (gameLoop.IsInitialized &&
                    !gameLoop.IsShutdown)
                {
                    gameLoop.Shutdown();
                }
            }
            finally
            {
                hostedApplication.Dispose();
            }
        }
    }

    /// <summary>
    /// Compatibility overload for games using EngineGame.
    /// </summary>
    public static void Run<TGame>(
        Func<EngineRuntime, TGame> createGame,
        DesktopGameHostOptions? options = null)
        where TGame : EngineGame
    {
        ArgumentNullException.ThrowIfNull(createGame);

        Run(
            context => createGame(context.Runtime),
            options);
    }

    private sealed class HostedApplication :
    IApplication,
    IDisposable
    {
        private readonly IApplication _inner;

        private bool _shutdown;
        private bool _disposed;

        public HostedApplication(IApplication inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            _inner = inner;
        }

        public void Initialize() =>
            _inner.Initialize();

        public void Update(
            Engine.Core.Time.TimeSnapshot time) =>
            _inner.Update(time);

        public void FixedUpdate(
            Engine.Core.Time.SimulationTime time) =>
            _inner.FixedUpdate(time);

        public void Render(double interpolationAlpha) =>
            _inner.Render(interpolationAlpha);

        public void Shutdown()
        {
            if (_shutdown)
            {
                return;
            }

            _shutdown = true;

            try
            {
                _inner.Shutdown();
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}