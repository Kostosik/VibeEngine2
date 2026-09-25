using Engine.Core.Time;

namespace Engine.Core.Application;

public sealed class GameLoop : IGameLoopController
{
    private static readonly Duration DefaultMaxFrameDelta =
        Duration.FromMilliseconds(250);

    private const int DefaultMaxFixedStepsPerFrame = 8;

    private readonly IApplication _application;
    private readonly IClock _clock;

    private bool _paused;
    private bool _stepRequested;

    public bool IsPaused =>
    _paused;

    private readonly Duration _tickDuration;
    private readonly Duration _maxFrameDelta;
    private readonly int _maxFixedStepsPerFrame;

    private Duration _elapsed = Duration.Zero;
    private Duration _accumulator = Duration.Zero;
    private SimulationRate _simulationRate;
    private Tick _tick = Tick.Zero;

    private bool _initialized;
    private bool _shutdown;

    public GameLoop(
        IApplication application,
        SimulationRate simulationRate)
        : this(
            application,
            simulationRate,
            new StopwatchClock(),
            DefaultMaxFrameDelta,
            DefaultMaxFixedStepsPerFrame)
    {
    }

    public GameLoop(
        IApplication application,
        SimulationRate simulationRate,
        IClock clock,
        Duration maxFrameDelta,
        int maxFixedStepsPerFrame)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(clock);

        if (simulationRate.TicksPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(simulationRate),
                "Simulation rate must be greater than zero.");
        }

        if (maxFrameDelta.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFrameDelta),
                "Maximum frame delta must be greater than zero.");
        }

        if (maxFixedStepsPerFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFixedStepsPerFrame),
                "Maximum fixed steps per frame must be greater than zero.");
        }

        _application = application;
        _clock = clock;
        _simulationRate = simulationRate;
        _tickDuration =
            simulationRate.TickDuration;

        _maxFrameDelta =
            maxFrameDelta;

        _maxFixedStepsPerFrame =
            maxFixedStepsPerFrame;
    }

    public bool IsInitialized =>
        _initialized;

    public bool IsShutdown =>
        _shutdown;

    public Tick Tick =>
        _tick;

    public Duration Elapsed =>
        _elapsed;

    public Duration Accumulator =>
        _accumulator;

    public Duration MaxFrameDelta =>
        _maxFrameDelta;

    public int MaxFixedStepsPerFrame =>
        _maxFixedStepsPerFrame;

    public double InterpolationAlpha
    {
        get
        {
            if (_paused)
            {
                return 1.0;
            }

            if (_tickDuration.IsZero)
            {
                return 0.0;
            }

            var alpha =
                _accumulator.TotalSeconds /
                _tickDuration.TotalSeconds;

            return System.Math.Clamp(
                alpha,
                0.0,
                1.0);
        }
    }

    public void Initialize()
    {
        if (_initialized)
        {
            throw new InvalidOperationException(
                "Game loop has already been initialized.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Game loop has already been shut down.");
        }

        _application.Initialize();

        _clock.Restart();

        _initialized = true;
    }

    public void Update()
    {
        EnsureInitialized();

        var delta =
            new Duration(
                _clock.Elapsed);

        _clock.Restart();

        Update(delta);
    }

    public void Update(
        Duration delta)
    {
        EnsureInitialized();

        if (delta.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delta),
                "Frame delta cannot be negative.");
        }

        if (delta.Value >
            _maxFrameDelta.Value)
        {
            delta = _maxFrameDelta;
        }

        _elapsed += delta;
        _accumulator += delta;

        var frameTime =
            new TimeSnapshot(
                delta,
                _elapsed,
                _tick);

        _application.Update(
            frameTime);

        if (_paused)
        {
            if (_stepRequested)
            {
                _stepRequested = false;

                ExecuteFixedStep();
            }

            return;
        }

        var fixedSteps = 0;

        while (_accumulator.Value >=
               _tickDuration.Value &&
               fixedSteps < _maxFixedStepsPerFrame)
        {
            _accumulator -= _tickDuration;

            ExecuteFixedStep();

            fixedSteps++;
        }



        if (_accumulator.Value >=
            _tickDuration.Value)
        {
            _accumulator =
                Duration.Zero;
        }
    }

    public void Render()
    {
        EnsureInitialized();

        _application.Render(
            InterpolationAlpha);
    }

    public void Shutdown()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Game loop has not been initialized.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Game loop has already been shut down.");
        }

        _clock.Stop();

        _application.Shutdown();

        _shutdown = true;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Game loop must be initialized before use.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Game loop has already been shut down.");
        }
    }

    public void Pause()
    {
        EnsureInitialized();

        if (_paused)
        {
            return;
        }

        _paused = true;
        _stepRequested = false;
        _accumulator = Duration.Zero;
    }

    public void Resume()
    {
        EnsureInitialized();

        if (!_paused)
        {
            return;
        }

        _paused = false;
        _stepRequested = false;
    }

    public bool RequestStep()
    {
        EnsureInitialized();

        if (!_paused)
        {
            return false;
        }

        _stepRequested = true;

        return true;
    }

    private void ExecuteFixedStep()
    {
        _tick++;

        _application.FixedUpdate(
            new SimulationTime(
                _simulationRate.SimulationDelta,
                _tick));
    }
}