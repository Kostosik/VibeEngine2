using Engine.Audio.Components;
using Engine.Core.Application;
using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Diagnostics;
using Engine.Core.Events;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Components;
using Engine.ECS.Systems;
using Engine.Graphics.Sprites;
using Engine.Graphics2D.Sprites;
using Engine.Particles;
using Engine.Physics;
using Engine.Runtime.Particles;
using Engine.Scripting;
using Engine.Simulations;
using Engine.Tooling.Console;
using Engine.Tooling.Debugging;
using Engine.Tooling.DebugVisualization;
using Engine.Tooling.Inspection;
using Engine.Tooling.Profiling;
using Engine.Tooling.Validation;
using Engine.Worlds.Spatial;
using System.Runtime.ExceptionServices;

namespace Engine.Runtime;

public sealed class EngineRuntime :
    IDisposable
{
    private bool _disposed;
    private DebugExecutionController? _debugExecution;
    private RebuildEngineDebugCommand? _rebuildEngineCommand;
    public ValidationService? Validation { get; private set; }
    public DebugVisualizationRenderer? DebugVisualizationRenderer { get; private set; }
    private SpriteRenderSystem? _spriteRenderSystem;
    public ScriptRegistry ScriptRegistry { get; }
    private AudioEcsSystem? _audioEcsSystem;
    public ScriptRuntime Scripts { get; }
    private ParticleRuntime? _particleRuntime;
    public EngineConsole? EngineConsole { get; private set; }
    public EngineRuntime(EngineRuntimeOptions options)
        : this(
            ValidateHeadlessOptions(options),
            new EngineRuntimeServices())
    {
    }

    private static EngineRuntimeOptions ValidateHeadlessOptions(
        EngineRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ToolingEnabled)
        {
            throw new ArgumentException(
                "Tooling requires configured runtime services. " +
                "Use the constructor that accepts EngineRuntimeServices.",
                nameof(options));
        }

        return options;
    }

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

        ScriptRegistry =
    new ScriptRegistry();

        Scripts =
            new ScriptRuntime(
                EcsWorld,
                ScriptRegistry,
                Events);

        Scheduler.Add(
            Scripts.UpdateSystem,
            SystemPhase.Update);

        Scheduler.Add(
            Scripts.FixedUpdateSystem,
            SystemPhase.FixedUpdate);

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

        var transformHierarchySystem =
    new TransformHierarchySystem(EcsWorld);

        Scheduler.Add(
            transformHierarchySystem,
            SystemPhase.FixedUpdate)
            .Before<PhysicsSystem2D>();

        SpatialSync =
            new SpatialSyncSystem(
                World,
                EcsWorld);

        Scheduler.Add(
            new PhysicsTransformSyncSystem(EcsWorld),
            SystemPhase.FixedUpdate)
            .After<PhysicsSystem2D>();

        Scheduler.Add(
            SpatialSync,
            SystemPhase.FixedUpdate)
            .After<PhysicsTransformSyncSystem>();

        Simulation =
            new Simulation(
                Scheduler,
                Commands,
                Events);

        Simulation.RegisterDeterministicState(
            EcsWorld);

        Simulation.RegisterDeterministicState(
            World);

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
            Scripts.ScriptFaulted += OnScriptFaulted;
            FatalError =
                new FatalError(
                    DiagnosticReporter);

            return;
        }

        Inspection =
            new WorldInspectionService(
                EcsWorld.Inspector);

        DebugRegistry =
            new DebugCommandRegistry();

        Console =
            new DebugConsole(
                DebugRegistry);
        EngineConsole =
    new EngineConsole(Console);

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

        var graphics =
Services.Graphics;

        DebugVisualizationRenderer =
    new DebugVisualizationRenderer(
        graphics);

        Validation =
    new ValidationService();

        Validation.Register(
            new WorldValidator(
                EcsWorld));
        Validation.Register(
    new PhysicsValidator(
        EcsWorld));

        _rebuildEngineCommand =
    new RebuildEngineDebugCommand();

        DebugRegistry.Register(
            _rebuildEngineCommand);

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

        var consoleDiagnosticSink =
            new ConsoleDiagnosticSink(Console!);

        var diagnosticSink =
            new CompositeDiagnosticSink(
                DiagnosticCollector,
                consoleDiagnosticSink);

        DiagnosticReporter =
            new DiagnosticReporter(
                diagnosticSink,
                "Engine");

        Scripts.ScriptFaulted += OnScriptFaulted;

        FatalError =
            new FatalError(
                DiagnosticReporter);
    }

    public Engine.ECS.World EcsWorld { get; }

    public Engine.Worlds.World World { get; }

    public EngineRuntimeServices Services { get; }

    public ParticleRuntime Particles
    {
        get
        {
            EnsureNotDisposed();

            if (Simulation.IsShutdown)
            {
                throw new InvalidOperationException(
                    "Cannot access particles after runtime shutdown.");
            }

            return _particleRuntime ??=
                new ParticleRuntime(
                    EcsWorld,
                    Services.Graphics,
                    Services.Content,
                    Services.Camera);
        }
    }

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

    public void Update(SystemContext context)
    {
        EnsureNotDisposed();

        Simulation.Update(context);

        _rebuildEngineCommand?.DrainOutput(Console);

        UpdateAudio(context.Time.Delta.TotalSeconds);
        if (_particleRuntime is null &&
            HasParticleEffectComponents())
        {
            _particleRuntime = new ParticleRuntime(
                EcsWorld,
                Services.Graphics,
                Services.Content,
                Services.Camera);
        }

        _particleRuntime?.Update(
            (float)context.Time.Delta.TotalSeconds);
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        EnsureNotDisposed();

        Simulation.FixedUpdate(
            context);
    }

    internal void RenderSprites()
    {
        EnsureNotDisposed();

        var enumerator =
            EcsWorld
                .Query<SpriteComponent, WorldTransform2D>()
                .GetEnumerator();

        if (!enumerator.MoveNext())
        {
            return;
        }

        _spriteRenderSystem ??=
            new SpriteRenderSystem(
                EcsWorld,
                Services.Graphics,
                Services.Content,
                Services.Camera);

        _spriteRenderSystem.Render();
    }

    internal void RenderParticles()
    {
        EnsureNotDisposed();

        _particleRuntime?.Render();
    }

    private void DisposeParticleRuntime()
    {
        var particles = _particleRuntime;
        _particleRuntime = null;

        particles?.Dispose();
    }

    private void DisposeSpriteRenderSystem()
    {
        var renderer = _spriteRenderSystem;
        _spriteRenderSystem = null;

        renderer?.Dispose();
    }

    private bool HasParticleEffectComponents()
    {
        var query = EcsWorld
            .Query<ParticleEffectComponent>()
            .GetEnumerator();

        return query.MoveNext();
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

    private void OnScriptFaulted(ScriptFault fault)
{
    DiagnosticReporter.Error(
        "Script.ExecutionFault",
        $"Script '{fault.ScriptId}' on entity '{fault.Entity}' " +
        $"failed during {fault.Phase}.",
        fault.Exception);
}

    private void UpdateAudio(double deltaSeconds)
    {
        if (_audioEcsSystem is null && !HasAudioComponents())
        {
            return;
        }

        _audioEcsSystem ??= new AudioEcsSystem(
            EcsWorld,
            Services.Audio);

        _audioEcsSystem.Update(deltaSeconds);
    }

    private bool HasAudioComponents()
    {
        var sources = EcsWorld
            .Query<AudioSourceComponent>()
            .GetEnumerator();

        if (sources.MoveNext())
        {
            return true;
        }

        var listeners = EcsWorld
            .Query<AudioListenerComponent>()
            .GetEnumerator();

        return listeners.MoveNext();
    }

    private void DisposeAudioEcsSystem()
    {
        var audioSystem = _audioEcsSystem;
        _audioEcsSystem = null;

        audioSystem?.Dispose();
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

        var errors = new List<Exception>();

        void TryCleanup(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        TryCleanup(Simulation.Shutdown);
        TryCleanup(DisposeAudioEcsSystem);
        TryCleanup(DisposeSpriteRenderSystem);
        TryCleanup(DisposeParticleRuntime);

        if (errors.Count == 1)
        {
            ExceptionDispatchInfo
                .Capture(errors[0])
                .Throw();
        }

        if (errors.Count > 1)
        {
            throw new AggregateException(
                "One or more engine runtime systems failed during shutdown.",
                errors);
        }
    }


    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var errors = new List<Exception>();

        void TryCleanup(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        // Освобождаем ссылки на игровой цикл и его команды.
        if (_debugExecution is not null)
        {
            TryCleanup(() => DebugRegistry?.Unregister("pause"));
            TryCleanup(() => DebugRegistry?.Unregister("resume"));
            TryCleanup(() => DebugRegistry?.Unregister("step"));
            TryCleanup(() => DebugRegistry?.Unregister("rebuild-engine"));

            var rebuildCommand = _rebuildEngineCommand;
            _rebuildEngineCommand = null;
            _debugExecution = null;

            TryCleanup(() => rebuildCommand?.Dispose());
        }

        // Завершаем Simulation, если она была запущена.
        if (Simulation.IsInitialized &&
            !Simulation.IsShutdown)
        {
            TryCleanup(Simulation.Shutdown);
        }

        // Каждая подсистема очищается независимо от остальных.
        TryCleanup(DisposeAudioEcsSystem);
        TryCleanup(DisposeSpriteRenderSystem);
        TryCleanup(DisposeParticleRuntime);

        TryCleanup(() => EngineConsole?.Dispose());
        TryCleanup(() => Console?.Dispose());
        TryCleanup(() => SchedulerProfiler?.Dispose());

        TryCleanup(() =>
        {
            Scripts.ScriptFaulted -= OnScriptFaulted;
        });

        TryCleanup(Scripts.Dispose);
        TryCleanup(Commands.Clear);
        TryCleanup(Events.Dispose);
        TryCleanup(Simulation.Dispose);
        TryCleanup(World.Dispose);
        TryCleanup(EcsWorld.Dispose);

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more engine runtime resources could not be released.",
                errors);
        }
    }


    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}