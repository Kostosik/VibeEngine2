using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Scripting;

namespace Engine.Tests.Scripting;

public sealed class ScriptRuntimeTests
{
    [Fact]
    public void ScriptRunsInUpdateAndFixedUpdate()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var script = new RecordingScript();

        var registry = new ScriptRegistry();

        registry.Register(
            "test.recording",
            () => script);

        using var runtime = new ScriptRuntime(
            world,
            registry);

        var scheduler = new SystemScheduler();

        scheduler.Add(
            runtime.UpdateSystem,
            SystemPhase.Update);

        scheduler.Add(
            runtime.FixedUpdateSystem,
            SystemPhase.FixedUpdate);

        scheduler.Build();

        runtime.Attach(
            entity,
            "test.recording");

        scheduler.Update(default);
        scheduler.FixedUpdate(default);

        Assert.Equal(1, script.AttachCount);
        Assert.Equal(1, script.UpdateCount);
        Assert.Equal(1, script.FixedUpdateCount);
        Assert.Equal(entity, script.Entity);
    }

    [Fact]
    public void DetachStopsFurtherExecution()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var script = new RecordingScript();

        var registry = new ScriptRegistry();

        registry.Register(
            "test.recording",
            () => script);

        using var runtime = new ScriptRuntime(
            world,
            registry);

        runtime.Attach(
            entity,
            "test.recording");

        Assert.True(
            runtime.Detach(
                entity,
                "test.recording"));

        runtime.UpdateSystem.Update(default);

        Assert.Equal(1, script.DetachCount);
        Assert.Equal(0, script.UpdateCount);
        Assert.Equal(0, runtime.InstanceCount);
    }

    [Fact]
    public void DestroyedEntityIsDetachedOnNextUpdate()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var script = new RecordingScript();

        var registry = new ScriptRegistry();

        registry.Register(
            "test.recording",
            () => script);

        using var runtime = new ScriptRuntime(
            world,
            registry);

        runtime.Attach(
            entity,
            "test.recording");

        world.DestroyEntity(entity);

        runtime.UpdateSystem.Update(default);

        Assert.Equal(1, script.DetachCount);
        Assert.Equal(0, script.UpdateCount);
        Assert.Equal(0, runtime.InstanceCount);
    }

    [Fact]
    public void FaultedScriptIsDisabledWithoutStoppingOtherScripts()
    {
        using var world = new World();

        var brokenEntity = world.CreateEntity();
        var healthyEntity = world.CreateEntity();

        var healthyScript = new RecordingScript();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.broken",
            () => new BrokenScript());

        registry.Register(
            "test.healthy",
            () => healthyScript);

        using var runtime = new ScriptRuntime(
            world,
            registry);

        var faults = new List<ScriptFault>();

        runtime.ScriptFaulted += faults.Add;

        runtime.Attach(
            brokenEntity,
            "test.broken");

        runtime.Attach(
            healthyEntity,
            "test.healthy");

        runtime.UpdateSystem.Update(default);
        runtime.UpdateSystem.Update(default);

        Assert.Equal(1, faults.Count);
        Assert.Equal(
            ScriptExecutionPhase.Update,
            faults[0].Phase);

        Assert.Equal(2, healthyScript.UpdateCount);
        Assert.Equal(2, runtime.InstanceCount);
    }

    [Fact]
    public void SameScriptCannotBeAttachedTwiceToEntity()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var registry = new ScriptRegistry();

        registry.Register<RecordingScript>(
            "test.recording");

        using var runtime = new ScriptRuntime(
            world,
            registry);

        runtime.Attach(
            entity,
            "test.recording");

        Assert.Throws<InvalidOperationException>(
            () => runtime.Attach(
                entity,
                "test.recording"));
    }

    public sealed class RecordingScript : IEntityScript
    {
        public int AttachCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int FixedUpdateCount { get; private set; }

        public int DetachCount { get; private set; }

        public EntityId Entity { get; private set; }

        public void OnAttach(ScriptContext context)
        {
            AttachCount++;
            Entity = context.Entity;
        }

        public void Update(
            ScriptContext context,
            Engine.Core.Time.TimeSnapshot time)
        {
            UpdateCount++;
        }

        public void FixedUpdate(
            ScriptContext context,
            Engine.Core.Time.SimulationTime time)
        {
            FixedUpdateCount++;
        }

        public void OnDetach(ScriptContext context)
        {
            DetachCount++;
        }
    }

    public sealed class BrokenScript : IEntityScript
    {
        public void Update(
            ScriptContext context,
            Engine.Core.Time.TimeSnapshot time)
        {
            throw new InvalidOperationException("Test failure.");
        }
    }
}