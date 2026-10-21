
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Entities;
using Engine.Runtime;

namespace Engine.Tests.Runtime;

public sealed class EngineGameRegistrationTests
{
    [Fact]
    public void ConfigureRuntime_RegistersGameSystemThatUsesCustomComponent()
    {
        using var runtime =
            new EngineRuntime(
                new EngineRuntimeOptions
                {
                    ToolingEnabled = false
                });

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new GameOwnedComponent());

        var system =
            new GameOwnedFixedSystem(
                runtime.EcsWorld,
                entity);

        var game =
            new RegistrationProbeGame(
                runtime,
                system);

        game.Initialize();

        game.FixedUpdate(
            new SimulationTime(
                Fixed32.FromRatio(1, 20),
                new Tick(1)));

        Assert.Equal(
            1,
            runtime.EcsWorld
                .Get<GameOwnedComponent>(entity)
                .UpdateCount);

        game.Shutdown();
    }

    private struct GameOwnedComponent
    {
        public int UpdateCount { get; set; }
    }

    private sealed class GameOwnedFixedSystem
        : IFixedUpdateSystem
    {
        private readonly Engine.ECS.World _world;
        private readonly EntityId _entity;

        public GameOwnedFixedSystem(
            Engine.ECS.World world,
            EntityId entity)
        {
            _world = world;
            _entity = entity;
        }

        public void FixedUpdate(
            FixedSystemContext context)
        {
            ref var component =
                ref _world.Get<GameOwnedComponent>(
                    _entity);

            component.UpdateCount++;
        }
    }

    private sealed class RegistrationProbeGame : EngineGame
    {
        private readonly GameOwnedFixedSystem _system;

        public RegistrationProbeGame(
            EngineRuntime runtime,
            GameOwnedFixedSystem system)
            : base(runtime)
        {
            _system = system;
        }

        protected override void ConfigureRuntime()
        {
            Runtime.Scheduler.Add(
                _system,
                SystemPhase.FixedUpdate);
        }
    }
}
