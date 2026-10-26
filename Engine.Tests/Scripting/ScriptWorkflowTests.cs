using System.Text.Json;

using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Entities;
using Engine.Runtime;
using Engine.Scripting;

namespace Engine.Tests.Scripting;

public sealed class ScriptWorkflowTests
{
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