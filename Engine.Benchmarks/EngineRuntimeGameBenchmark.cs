
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.Runtime;

namespace Engine.Benchmarks;

internal static class EngineRuntimeGameBenchmark
{
    private const int EntityCount = 10_000;
    private const int WarmupFrames = 120;
    private const int MeasuredFrames = 600;

    public static void Run()
    {
        EngineRuntime? runtime = null;

        ulong updateTick = 0;
        ulong fixedUpdateTick = 0;

        var updateDelta =
            Duration.FromSeconds(1.0 / 60.0);

        var fixedDelta =
            Fixed32.FromRatio(1, 20);

        GameBenchmarkRunner.Run(
            scenarioName: "Engine.Runtime ECS gameplay",
            initialize: () =>
            {
                runtime = new EngineRuntime(
                    new EngineRuntimeOptions
                    {
                        ToolingEnabled = false
                    });

                try
                {
                    var world = runtime.EcsWorld;

                    for (var i = 0; i < EntityCount; i++)
                    {
                        var entity = world.CreateEntity();

                        world.Add(
                            entity,
                            new BenchmarkActorComponent
                            {
                                Position = i % 100,
                                Velocity = 1.0f + (i % 10) * 0.1f
                            });
                    }

                    runtime.Scheduler.Add(
                        new BenchmarkUpdateSystem(world),
                        SystemPhase.Update);

                    runtime.Scheduler.Add(
                        new BenchmarkFixedUpdateSystem(world),
                        SystemPhase.FixedUpdate);

                    runtime.Initialize();
                }
                catch
                {
                    runtime.Dispose();
                    runtime = null;
                    throw;
                }
            },
            update: () =>
            {
                var activeRuntime = runtime
                    ?? throw new InvalidOperationException(
                        "Runtime has not been initialized.");

                updateTick++;

                activeRuntime.Update(
                    new SystemContext(
                        new TimeSnapshot(
                            updateDelta,
                            Duration.FromSeconds(
                                updateTick / 60.0),
                            new Tick(updateTick))));
            },
            fixedUpdate: () =>
            {
                var activeRuntime = runtime
                    ?? throw new InvalidOperationException(
                        "Runtime has not been initialized.");

                fixedUpdateTick++;

                activeRuntime.FixedUpdate(
                    new FixedSystemContext(
                        new SimulationTime(
                            fixedDelta,
                            new Tick(fixedUpdateTick))));
            },
            shutdown: () =>
            {
                if (runtime is null)
                {
                    return;
                }

                var activeRuntime = runtime;
                runtime = null;

                try
                {
                    if (activeRuntime.IsInitialized &&
                        !activeRuntime.IsShutdown)
                    {
                        activeRuntime.Shutdown();
                    }
                }
                finally
                {
                    activeRuntime.Dispose();
                }
            },
            warmupFrames: WarmupFrames,
            measuredFrames: MeasuredFrames,
            fixedUpdatesPerFrame: 1);
    }

    private struct BenchmarkActorComponent
    {
        public float Position;
        public float Velocity;
        public long UpdateCount;
        public long FixedUpdateCount;
    }

    private sealed class BenchmarkUpdateSystem :
        IUpdateSystem
    {
        private readonly World _world;

        public BenchmarkUpdateSystem(World world)
        {
            _world = world;
        }

        public void Update(SystemContext context)
        {
            _world.ClearDirty<BenchmarkActorComponent>();

            var delta =
                (float)context.Time.Delta.TotalSeconds;

            foreach (var item in
                     _world.Query<BenchmarkActorComponent>())
            {
                ref var actor = ref item.Component;

                actor.Position += actor.Velocity * delta;
                actor.UpdateCount++;
            }
        }
    }

    private sealed class BenchmarkFixedUpdateSystem :
        IFixedUpdateSystem
    {
        private readonly World _world;

        public BenchmarkFixedUpdateSystem(World world)
        {
            _world = world;
        }

        public void FixedUpdate(FixedSystemContext context)
        {
            _world.ClearDirty<BenchmarkActorComponent>();

            var delta = context.Time.Delta.ToFloat();

            foreach (var item in
                     _world.Query<BenchmarkActorComponent>())
            {
                ref var actor = ref item.Component;

                actor.Position += actor.Velocity * delta;
                actor.FixedUpdateCount++;
            }
        }
    }
}
