using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics;
using Engine.Physics.Collision;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using Engine.Runtime;
using Engine.Scripting;
using System.Text.Json;
using Engine.Core.Math;

namespace Engine.Tests.Scripting;

public sealed class ScriptWorkflowTests
{
    private struct PlayerTag
    {
    }
    [Fact]
    public void GameScript_CanReactToSeparatePhysicsTrigger()
    {


        var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false,
                Physics = new PhysicsSettings2D
                {
                    Gravity = FixedVector2.Zero,
                    Substeps = 1,
                    PositionIterations = 0,
                    VelocityIterations = 0
                }
            });

        runtime.ScriptRegistry.Register(
            "game.door",
            static () => new DoorTriggerScript());

        runtime.Initialize();

        var door = runtime.EcsWorld.CreateEntity();
        var trigger = runtime.EcsWorld.CreateEntity();
        var player = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            door,
            new DoorState());

        runtime.EcsWorld.Add(
            trigger,
            new WorldTransform2D(FixedVector2.Zero));

        runtime.EcsWorld.Add(
            trigger,
            PhysicsBody2D.Static());

        runtime.EcsWorld.Add(
            trigger,
            new Collider2D(
                new CircleShape2D(Fixed32.FromInt(2)))
            {
                IsTrigger = true
            });

        runtime.EcsWorld.Add(
            player,
            new WorldTransform2D(FixedVector2.Zero));
        runtime.EcsWorld.Add(
    player,
    new PlayerTag());
        runtime.EcsWorld.Add(
            player,
            PhysicsBody2D.Dynamic(Fixed32.One));

        runtime.EcsWorld.Add(
            player,
            new Collider2D(
                new CircleShape2D(Fixed32.One)));

        var configuration = JsonSerializer.Serialize(
            new
            {
                triggerIndex = trigger.Index,
                triggerGeneration = trigger.Generation
            });

        runtime.Scripts.Attach(
            door,
            "game.door",
            configuration);

        runtime.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromFloat(1f / 60f),
                    Tick.Zero)));

        Assert.True(
            runtime.EcsWorld.Get<DoorState>(door).IsOpen);

        ref var triggerTransform =
    ref runtime.EcsWorld.Get<WorldTransform2D>(trigger);

        triggerTransform.Position = FixedVector2.Zero;
        runtime.FixedUpdate(
    new FixedSystemContext(
        new SimulationTime(
            Fixed32.FromFloat(1f / 60f),
            new Tick(1))));

        Assert.True(
            runtime.EcsWorld.Get<DoorState>(door).IsOpen);
    }

    private struct DoorState
    {
        public bool IsOpen;
    }

    private sealed class DoorTriggerScript : IEntityScript
    {
        private EntityId _trigger;

        public void OnAttach(ScriptContext context)
        {
            using var document = JsonDocument.Parse(
                context.ConfigurationJson);

            var root = document.RootElement;

            _trigger = new EntityId(
                root.GetProperty("triggerIndex").GetUInt32(),
                root.GetProperty("triggerGeneration").GetUInt32());

            context.Subscribe<PhysicsContactEvent>(
                contact =>
                {
                    if (contact.Type != PhysicsContactType.Trigger ||
                        contact.Phase != PhysicsContactPhase.Enter)
                    {
                        return;
                    }



                    var pair = contact.Manifold.Pair;

                    if (pair.First != _trigger &&
                        pair.Second != _trigger)
                    {
                        return;
                    }

                    var otherEntity =
                        pair.First == _trigger
                            ? pair.Second
                            : pair.First;

                    if (!context.Has<PlayerTag>(otherEntity))
                    {
                        return;
                    }

                    ref var state = ref context.Get<DoorState>();

                    state.IsOpen = true;
                });
        }
    }

    [Fact]
    public void GameCode_CanRegisterAssignAndExecuteScript()
    {
        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            });

        var game = new ScriptWorkflowGame(runtime);

        game.Initialize();

        // Обычный кадр игрового runtime.
        runtime.Update(new SystemContext(default));

        // Шаг фиксированной симуляции.
        game.FixedUpdate(default);

        var state = runtime.EcsWorld.Get<ScriptState>(
            game.Entity);

        Assert.Equal(2, state.UpdateCount);
        Assert.Equal(1, state.FixedUpdateCount);

        Assert.True(
            runtime.EcsWorld.Has<ScriptAssignmentsComponent>(
                game.Entity));

        var assignment = Assert.Single(
            runtime.EcsWorld
                .Get<ScriptAssignmentsComponent>(game.Entity)
                .Assignments
                .ToArray());

        Assert.Equal(
            "game.counter",
            assignment.ScriptId);

        Assert.Equal(
            """{"step":2}""",
            assignment.ConfigurationJson);

        game.Shutdown();
    }

    private sealed class ScriptWorkflowGame : EngineGame
    {
        public EntityId Entity { get; private set; }

        public ScriptWorkflowGame(EngineRuntime runtime)
            : base(runtime)
        {
        }

        protected override void ConfigureRuntime()
        {
            Runtime.ScriptRegistry.Register(
                "game.counter",
                static () => new CounterScript());
        }

        protected override void OnInitialize()
        {
            Entity = Runtime.EcsWorld.CreateEntity();

            Runtime.EcsWorld.Add(
                Entity,
                new ScriptState());

            Runtime.Scripts.Attach(
                Entity,
                "game.counter",
                """{"step":2}""");
        }
    }

    private struct ScriptState
    {
        public int UpdateCount;
        public int FixedUpdateCount;
    }

    private sealed class CounterScript : IEntityScript
    {
        private int _step;

        public void OnAttach(ScriptContext context)
        {
            using var document = JsonDocument.Parse(
                context.ConfigurationJson);

            _step = document.RootElement
                .GetProperty("step")
                .GetInt32();
        }

        public void Update(
            ScriptContext context,
            TimeSnapshot time)
        {
            ref var state = ref context.Get<ScriptState>();
            state.UpdateCount += _step;
        }

        public void FixedUpdate(
            ScriptContext context,
            SimulationTime time)
        {
            ref var state = ref context.Get<ScriptState>();
            state.FixedUpdateCount++;
        }
    }
}