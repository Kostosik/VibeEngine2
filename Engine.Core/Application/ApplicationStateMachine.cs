namespace Engine.Core.Application;

public sealed class ApplicationStateMachine
{
    private readonly ApplicationStateContext _context;

    private IApplicationState? _currentState;
    private IApplicationState? _pendingState;

    private bool _initialized;
    private bool _shutdown;

    public ApplicationStateMachine()
    {
        _context =
            new ApplicationStateContext(
                RequestStateChange);
    }

    public IApplicationState? CurrentState =>
        _currentState;

    public bool IsInitialized =>
        _initialized;

    public bool IsShutdown =>
        _shutdown;

    public void Initialize(
        IApplicationState initialState)
    {
        ArgumentNullException.ThrowIfNull(initialState);

        if (_initialized)
        {
            throw new InvalidOperationException(
                "Application state machine has already been initialized.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Application state machine has already been shut down.");
        }

        _currentState = initialState;

        _currentState.Initialize(_context);

        _initialized = true;
    }

    public void Update(
        Engine.Core.Time.TimeSnapshot time)
    {
        EnsureRunning();

        _currentState!.Update(time);

        ApplyPendingState();
    }

    public void FixedUpdate(
        Engine.Core.Time.SimulationTime time)
    {
        EnsureRunning();

        _currentState!.FixedUpdate(time);

        ApplyPendingState();
    }

    public void Render(
        double interpolationAlpha)
    {
        EnsureRunning();

        _currentState!.Render(
            interpolationAlpha);

        ApplyPendingState();
    }

    public void ChangeState(
        IApplicationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        EnsureRunning();

        _pendingState = state;
    }

    public void Shutdown()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Application state machine has not been initialized.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Application state machine has already been shut down.");
        }

        _pendingState = null;

        _currentState?.Shutdown();
        _currentState = null;

        _shutdown = true;
    }

    private void RequestStateChange(
        IApplicationState state)
    {
        _pendingState = state;
    }

    private void ApplyPendingState()
    {
        if (_pendingState is null)
            return;

        var nextState = _pendingState;

        _pendingState = null;

        var previousState = _currentState;

        previousState?.Shutdown();

        _currentState = null;

        nextState.Initialize(_context);

        _currentState = nextState;
    }

    private void EnsureRunning()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Application state machine has not been initialized.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Application state machine has already been shut down.");
        }

        if (_currentState is null)
        {
            throw new InvalidOperationException(
                "Application state machine has no current state.");
        }
    }
}