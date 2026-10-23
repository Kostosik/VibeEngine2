using Engine.Core.Application;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Input;

namespace Engine.Runtime;

/// <summary>
/// Base class for a user-defined game.
/// The host creates EngineRuntime and supplies it to the game.
/// </summary>
public abstract class EngineGame : Application
{
    private bool _initializationStarted;
    private bool _initialized;
    private bool _shutdown;

    protected EngineGame(EngineRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        Runtime = runtime;
    }

    protected InputActionMap InputActions =>
    Runtime.Services.InputActions;

    /// <summary>
    /// The engine runtime available to the game.
    /// </summary>
    public EngineRuntime Runtime { get; }

    /// <summary>
    /// Configure game-specific systems before runtime initialization.
    /// </summary>
    protected virtual void ConfigureRuntime()
    {
    }

    /// <summary>
    /// Called after the engine runtime has been initialized.
    /// </summary>
    protected virtual void OnInitialize()
    {
    }

    /// <summary>
    /// Called once per frame, before the engine update systems.
    /// </summary>
    protected virtual void OnUpdate(TimeSnapshot time)
    {
    }

    /// <summary>
    /// Called before the engine fixed-update systems.
    /// </summary>
    protected virtual void OnFixedUpdate(SimulationTime time)
    {
    }

    /// <summary>
    /// Called for rendering while the host's graphics frame is active.
    /// </summary>
    protected virtual void OnRender(double interpolationAlpha)
    {
    }

    /// <summary>
    /// Called before the engine runtime is shut down.
    /// </summary>
    protected virtual void OnShutdown()
    {
    }

    public sealed override void Initialize()
    {
        if (_initializationStarted)
        {
            throw new InvalidOperationException(
                "Game initialization has already been started.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "The game has already been shut down.");
        }

        _initializationStarted = true;

        try
        {
            ConfigureRuntime();

            Runtime.Initialize();

            OnInitialize();

            _initialized = true;
        }
        catch
        {
            try
            {
                OnShutdown();
            }
            catch
            {
                // Не подменяем исходную ошибку инициализации
                // исключением, возникшим во время очистки.
            }
            finally
            {
                ShutdownRuntimeSafely();
                _shutdown = true;
            }

            throw;
        }
    }

    public sealed override void Update(TimeSnapshot time)
    {
        EnsureActive();

        Runtime.Services.Input.Update();

        OnUpdate(time);

        Runtime.Update(
            new SystemContext(time));
    }

    public sealed override void FixedUpdate(SimulationTime time)
    {
        EnsureActive();

        OnFixedUpdate(time);

        Runtime.FixedUpdate(
            new FixedSystemContext(time));
    }

    public sealed override void Render(double interpolationAlpha)
    {
        EnsureActive();

        OnRender(interpolationAlpha);

        Runtime.RenderSprites();
        Runtime.RenderParticles();
    }

    public sealed override void Shutdown()
    {
        if (!_initialized || _shutdown)
        {
            return;
        }

        try
        {
            OnShutdown();
        }
        finally
        {
            try
            {
                ShutdownRuntimeSafely();
            }
            finally
            {
                _initialized = false;
                _shutdown = true;
            }
        }
    }

    private void EnsureActive()
    {
        if (!_initialized || _shutdown)
        {
            throw new InvalidOperationException(
                "The game must be initialized and running.");
        }
    }

    private void ShutdownRuntimeSafely()
    {
        if (!Runtime.IsInitialized || Runtime.IsShutdown)
        {
            return;
        }

        try
        {
            Runtime.Shutdown();
        }
        catch
        {
            // Preserve the original initialization or shutdown exception.
        }
    }
}