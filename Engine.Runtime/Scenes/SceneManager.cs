using Engine.Core.Time;
using System.Runtime.ExceptionServices;

namespace Engine.Runtime.Scenes;

/// <summary>
/// Manages the lifecycle and transitions of runtime scenes.
/// </summary>
public sealed class SceneManager<TContext> : IDisposable
    where TContext : class
{
    private readonly TContext _context;

    private IScene<TContext>? _pendingScene;
    private IScene<TContext>? _initializingScene;

    private bool _started;
    private bool _disposed;
    private bool _cleaningUp;

    public IScene<TContext>? ActiveScene { get; private set; }

    public SceneManager(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    /// <summary>
    /// Starts the scene manager with its initial scene.
    /// </summary>
    public void Start(IScene<TContext> initialScene)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(initialScene);

        if (_started)
        {
            throw new InvalidOperationException(
                "Scene manager has already been started.");
        }

        // Временно разрешаем RequestScene()
        // во время Initialize() первой сцены.
        _started = true;

        try
        {
            Activate(initialScene);
        }
        catch
        {
            // Первая сцена не запустилась.
            // Разрешаем повторный запуск менеджера.
            _started = false;

            throw;
        }
    }

    /// <summary>
    /// Requests a scene transition.
    /// The transition is performed after the current Update finishes.
    /// </summary>
    public void RequestScene(IScene<TContext> scene)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(scene);

        if (!_started)
        {
            throw new InvalidOperationException(
                "Scene manager has not been started.");
        }

        if (_cleaningUp)
        {
            throw new InvalidOperationException(
                "Scene transitions are not allowed during cleanup.");
        }

        if (ReferenceEquals(scene, ActiveScene) ||
            ReferenceEquals(scene, _initializingScene))
        {
            throw new InvalidOperationException(
                "Cannot request the currently active or initializing scene.");
        }

        if (_pendingScene is not null)
        {
            throw new InvalidOperationException(
                "A scene transition is already pending.");
        }

        _pendingScene = scene;
    }

    /// <summary>
    /// Updates the active scene and processes pending transitions.
    /// </summary>
    public void Update(TimeSnapshot time)
    {
        EnsureRunning();

        ActiveScene!.Update(time);

        if (_pendingScene is null)
        {
            return;
        }

        var nextScene = _pendingScene;
        _pendingScene = null;

        Activate(nextScene);
    }

    /// <summary>
    /// Renders the active scene.
    /// </summary>
    public void Render(double interpolationAlpha)
    {
        EnsureRunning();

        ActiveScene!.Render(interpolationAlpha);
    }

    private void Activate(IScene<TContext> scene)
    {
        _initializingScene = scene;

        try
        {
            scene.Initialize(
                _context,
                RequestScene);
        }
        catch (Exception initializationException)
        {
            var errors = new List<Exception>
            {
                initializationException
            };

            var pendingScene = _pendingScene;
            _pendingScene = null;

            _cleaningUp = true;

            try
            {
                TryDispose(scene, errors);

                if (pendingScene is not null)
                {
                    TryDispose(pendingScene, errors);
                }
            }
            finally
            {
                _cleaningUp = false;
            }

            if (errors.Count == 1)
            {
                throw;
            }

            throw new AggregateException(
                "Scene initialization and cleanup failed.",
                errors);
        }
        finally
        {
            _initializingScene = null;
        }

        var previousScene = ActiveScene;

        ActiveScene = scene;

        previousScene?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var pendingScene = _pendingScene;
        _pendingScene = null;

        var activeScene = ActiveScene;
        ActiveScene = null;

        var errors = new List<Exception>();

        if (pendingScene is not null)
        {
            TryDispose(pendingScene, errors);
        }

        if (activeScene is not null)
        {
            TryDispose(activeScene, errors);
        }

        ThrowCleanupErrors(errors);
    }

    private static void TryDispose(
        IScene<TContext> scene,
        List<Exception> errors)
    {
        try
        {
            scene.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

    private void EnsureRunning()
    {
        EnsureNotDisposed();

        if (!_started || ActiveScene is null)
        {
            throw new InvalidOperationException(
                "Scene manager is not running.");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static void ThrowCleanupErrors(
        List<Exception> errors)
    {
        if (errors.Count == 1)
        {
            ExceptionDispatchInfo
                .Capture(errors[0])
                .Throw();
        }

        if (errors.Count > 1)
        {
            throw new AggregateException(
                "One or more scenes failed during disposal.",
                errors);
        }
    }
}