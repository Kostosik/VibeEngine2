using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Events;
using Engine.Core.Simulations;
using Engine.Core.Systems;

namespace Engine.Simulations;

public sealed class Simulation : ISimulation
{
    private readonly CommandDispatcher _commandDispatcher;

    private bool _initialized;
    private bool _shutdown;
    private readonly List<IDeterministicState> _deterministicStates = new();
    public Simulation(
        SystemScheduler scheduler,
        CommandQueue commands,
        EventBus events)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(events);

        Systems = scheduler;
        Commands = commands;
        Events = events;

        _commandDispatcher =
            new CommandDispatcher(
                Commands);
    }

    public SystemScheduler Systems { get; }

    public CommandQueue Commands { get; }

    public EventBus Events { get; }

    public CommandDispatcher CommandDispatcher =>
        _commandDispatcher;

    public bool IsInitialized =>
        _initialized;

    public bool IsShutdown =>
        _shutdown;

    public void Submit(
    ICommand command)
    {
        EnsureInitialized();

        ArgumentNullException.ThrowIfNull(
            command);

        Commands.Enqueue(
            command);
    }

    public void RegisterDeterministicState(
    IDeterministicState state)
    {
        EnsureInitializedForRegistration();

        ArgumentNullException.ThrowIfNull(state);

        _deterministicStates.Add(state);
    }

    private void EnsureInitializedForRegistration()
    {
        if (_initialized)
        {
            throw new InvalidOperationException(
                "Deterministic state cannot be registered after simulation initialization.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Simulation has already been shut down.");
        }
    }

    public DeterministicStateHash GetStateHash()
    {
        EnsureInitialized();

        var hasher =
            DeterministicStateHasher.Create();

        foreach (var state in _deterministicStates)
        {
            state.AddToHash(
                ref hasher);
        }

        return hasher.GetHash();
    }

    public void Initialize()
    {
        if (_initialized)
        {
            throw new InvalidOperationException(
                "Simulation has already been initialized.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Simulation has already been shut down.");
        }

        Systems.Build();

        _initialized = true;
    }

    public void Update(
        SystemContext context)
    {
        EnsureInitialized();

        Systems.Update(context);

        Events.DispatchPending();
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        EnsureInitialized();

        _commandDispatcher.DispatchPending();

        Systems.FixedUpdate(context);

        Events.DispatchPending();
    }

    public void Shutdown()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Simulation has not been initialized.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Simulation has already been shut down.");
        }

        _shutdown = true;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Simulation must be initialized before use.");
        }

        if (_shutdown)
        {
            throw new InvalidOperationException(
                "Simulation has already been shut down.");
        }
    }
}