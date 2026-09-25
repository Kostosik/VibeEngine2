using Engine.Core.Application;
using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Diagnostics;
using Engine.Core.Events;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.Physics;
using Engine.Simulations;
using Engine.Tooling.Debugging;
using Engine.Tooling.DebugVisualization;
using Engine.Tooling.Inspection;
using Engine.Tooling.Profiling;
using Engine.Tooling.Validation;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Runtime;

public sealed class EngineRuntime :
    IDisposable
{
    private bool _disposed;
    private DebugExecutionController? _debugExecution;
    public ValidationService? Validation { get; private set; }
    public DebugVisualizationRenderer? DebugVisualizationRenderer { get; private set; }
    public EngineRuntime(
        EngineRuntimeOptions options,
        EngineRuntimeServices services)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            services);



        EcsWorld =
            new Engine.ECS.World();

        Services =
            services;

        DiagnosticCollector =
            new DiagnosticCollector();

        World =
            new Engine.Worlds.World(
                options.ChunkSize,
                EcsWorld);

        Commands =
            new CommandQueue();

        Events =
            new EventBus();

        Scheduler =
            new SystemScheduler();

        PhysicsSettings =
            options.Physics;

        Physics =
            new PhysicsSystem2D(
                EcsWorld,
                PhysicsSettings,
                Events);

        Scheduler.Add(
            Physics,
            SystemPhase.FixedUpdate);

        SpatialSync =
            new SpatialSyncSystem(
                World,
                EcsWorld);

        Scheduler.Add(
            SpatialSync,
            SystemPhase.FixedUpdate)
            .After<PhysicsSystem2D>();

        Simulation =
            new Simulation(
                Scheduler,
                Commands,
                Events);

        Simulation.RegisterDeterministicState(
            EcsWorld);

        Simulation.RegisterDeterministicState(
            PhysicsSettings);

        SimulationRate =
            options.SimulationRate;

        if (!options.ToolingEnabled)
        {
            DiagnosticReporter =
                new DiagnosticReporter(
                    DiagnosticCollector,
                    "Engine");

            FatalError =
                new FatalError(
                    DiagnosticReporter);

            return;
        }

        Validation =
    new ValidationService();

        Validation.Register(
            new WorldValidator(
                EcsWorld));

        Inspection =
            new WorldInspectionService(
                EcsWorld.Inspector);

        DebugRegistry =
            new DebugCommandRegistry();

        Console =
            new DebugConsole(
                DebugRegistry);

        Profiler =
            new Profiler();

        SchedulerProfiler =
            new SchedulerProfiler(
                Scheduler,
                Profiler);

        PhysicsDebugVisualizer =
            new PhysicsDebugVisualizer(
                EcsWorld);

        DebugDrawList =
            new DebugDrawList();

        DebugVisualization =
            new DebugVisualizationService(
                DebugDrawList);

        DebugVisualizationRenderer =
    new DebugVisualizationRenderer(
        Services.Graphics);

        Validation =
    new ValidationService();

        Validation.Register(
            new WorldValidator(
                EcsWorld));

        DebugRegistry.Register(
            new ValidateDebugCommand(
                Validation));

        DebugVisualization.AddProvider(
            PhysicsDebugVisualizer);

        DebugRegistry.Register(
            new HelpDebugCommand(
                DebugRegistry));

        DebugRegistry.Register(
            new EntitiesDebugCommand(
                Inspection));

        DebugRegistry.Register(
            new InspectDebugCommand(
                Inspection));

        DebugRegistry.Register(
            new MetricsDebugCommand(
                Profiler));

        DebugRegistry.Register(
            new PhysicsDebugCommand(
                PhysicsDebugVisualizer));

        Console.AddSink(
            new SystemConsoleSink());

        Logger =
            new ConsoleLogger(
                Console);

        var diagnosticLogger =
            new DiagnosticLogger(
                Logger);

        var diagnosticSink =
            new CompositeDiagnosticSink(
                DiagnosticCollector,
                diagnosticLogger);

        DiagnosticReporter =
            new DiagnosticReporter(
                diagnosticSink,
                "Engine");

        FatalError =
            new FatalError(
                DiagnosticReporter);
    }

    public Engine.ECS.World EcsWorld { get; }

    public Engine.Worlds.World World { get; }

    public EngineRuntimeServices Services { get; }

    public CommandQueue Commands { get; }

    public EventBus Events { get; }

    public SystemScheduler Scheduler { get; }

    public Simulation Simulation { get; }

    public SimulationRate SimulationRate { get; }

    public PhysicsSettings2D PhysicsSettings { get; }

    public PhysicsSystem2D Physics { get; }

    public SpatialSyncSystem SpatialSync { get; }

    public DiagnosticCollector DiagnosticCollector { get; }

    public DiagnosticReporter DiagnosticReporter { get; }

    public FatalError FatalError { get; }

    public WorldInspectionService? Inspection { get; private set; }

    public DebugCommandRegistry? DebugRegistry { get; private set; }

    public DebugConsole? Console { get; private set; }

    public ConsoleLogger? Logger { get; private set; }

    public Profiler? Profiler { get; private set; }

    public SchedulerProfiler? SchedulerProfiler { get; private set; }

    public PhysicsDebugVisualizer? PhysicsDebugVisualizer { get; private set; }

    public DebugDrawList? DebugDrawList { get; private set; }

    public DebugVisualizationService? DebugVisualization { get; private set; }

    public DebugExecutionController? DebugExecution =>
        _debugExecution;

    public bool IsInitialized =>
        Simulation.IsInitialized;

    public bool IsShutdown =>
        Simulation.IsShutdown;

    public void Initialize()
    {
        EnsureNotDisposed();

        Simulation.Initialize();
    }

    public void Update(
        SystemContext context)
    {
        EnsureNotDisposed();

        Simulation.Update(
            context);
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        EnsureNotDisposed();

        Simulation.FixedUpdate(
            context);
    }

    public void RenderDebugVisualization()
    {
        EnsureNotDisposed();

        if (DebugVisualization is null ||
            DebugVisualizationRenderer is null)
        {
            return;
        }

        DebugVisualization.Build();

        DebugVisualizationRenderer.Render(
            DebugVisualization.DrawList);
    }

    public DeterministicStateHash GetStateHash()
    {
        EnsureNotDisposed();

        return Simulation.GetStateHash();
    }

    public void AttachGameLoopController(
        IGameLoopController loop)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            loop);

        if (DebugRegistry is null)
        {
            throw new InvalidOperationException(
                "Tooling is disabled.");
        }

        if (_debugExecution is not null)
        {
            throw new InvalidOperationException(
                "Game loop controller is already attached.");
        }

        _debugExecution =
            new DebugExecutionController(
                loop);

        DebugRegistry.Register(
            new PauseDebugCommand(
                _debugExecution));

        DebugRegistry.Register(
            new ResumeDebugCommand(
                _debugExecution));

        DebugRegistry.Register(
            new StepDebugCommand(
                _debugExecution));
    }

    public void Shutdown()
    {
        EnsureNotDisposed();

        Simulation.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (Simulation.IsInitialized &&
            !Simulation.IsShutdown)
        {
            Simulation.Shutdown();
        }

        SchedulerProfiler?.Dispose();

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}