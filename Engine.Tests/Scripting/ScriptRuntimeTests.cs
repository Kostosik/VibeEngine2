using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Scripting;

namespace Engine.Tests.Scripting;

public sealed class ScriptRuntimeTests
{
    [Fact]
    public void FactoryFailureRemovesAssignmentAndOtherScriptsStillRun()
    {
        using var world = new World();

        var brokenEntity = world.CreateEntity();
        var healthyEntity = world.CreateEntity();

        var healthyScript = new RecordingScript();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.factory-failing",
            () => throw new InvalidOperationException(
                "Intentional factory failure."));

        registry.Register(
            "test.healthy",
            () => healthyScript);

        world.Add(
            brokenEntity,
            new ScriptAssignmentsComponent(
                new[]
                {
                new ScriptAssignment(
                    "test.factory-failing",
                    "{}")
                }));

        world.Add(
            healthyEntity,
            new ScriptAssignmentsComponent(
                new[]
                {
                new ScriptAssignment(
                    "test.healthy",
                    "{}")
                }));

        using var runtime = new ScriptRuntime(
            world,
            registry);

        var faults = new List<ScriptFault>();
        runtime.ScriptFaulted += faults.Add;

        runtime.UpdateSystem.Update(default);

        Assert.False(
            world.Has<ScriptAssignmentsComponent>(
                brokenEntity));

        Assert.Equal(1, healthyScript.UpdateCount);
        Assert.Equal(1, runtime.InstanceCount);

        var fault = Assert.Single(faults);

        Assert.Equal(
            ScriptExecutionPhase.Attach,
            fault.Phase);

        Assert.Equal(
            "test.factory-failing",
            fault.ScriptId);
    }

    [Fact]
    public void FaultedFixedUpdateScriptIsDisabledWithoutStoppingOthers()
    {
        using var world = new World();

        var brokenEntity = world.CreateEntity();
        var healthyEntity = world.CreateEntity();

        var healthyScript = new RecordingScript();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.fixed-broken",
            () => new BrokenFixedUpdateScript());

        registry.Register(
            "test.fixed-healthy",
            () => healthyScript);

        using var runtime = new ScriptRuntime(
            world,
            registry);

        var faults = new List<ScriptFault>();
        runtime.ScriptFaulted += faults.Add;

        runtime.Attach(brokenEntity, "test.fixed-broken");
        runtime.Attach(healthyEntity, "test.fixed-healthy");

        runtime.FixedUpdateSystem.FixedUpdate(default);

        Assert.Equal(1, healthyScript.FixedUpdateCount);

        var fault = Assert.Single(faults);

        Assert.Equal(
            ScriptExecutionPhase.FixedUpdate,
            fault.Phase);

        Assert.Equal(2, runtime.InstanceCount);
    }

    [Fact]
    public void AttachFailureRollsBackAssignmentAndInstance()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var registry = new ScriptRegistry();

        registry.Register<AttachFailingScript>(
            "test.attach-failing");

        using var runtime = new ScriptRuntime(
            world,
            registry);

        Assert.Throws<InvalidOperationException>(
            () => runtime.Attach(
                entity,
                "test.attach-failing"));

        Assert.Equal(0, runtime.InstanceCount);

        Assert.False(
            world.Has<ScriptAssignmentsComponent>(entity));
    }

    [Fact]
    public void DisposeContinuesDetachingWhenFaultSubscriberThrows()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var healthyScript = new RecordingScript();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.healthy",
            () => healthyScript);

        registry.Register<DetachFailingScript>(
            "test.detach-failing");

        using var runtime = new ScriptRuntime(
            world,
            registry);

        runtime.ScriptFaulted += _ =>
            throw new InvalidOperationException(
                "Intentional subscriber failure.");

        runtime.Attach(entity, "test.healthy");
        runtime.Attach(entity, "test.detach-failing");

        runtime.Dispose();

        Assert.Equal(0, runtime.InstanceCount);
        Assert.Equal(1, healthyScript.DetachCount);
    }


    [Fact]
    public void FaultEventSubscriberFailureDoesNotStopOtherScripts()
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

        runtime.ScriptFaulted += _ =>
            throw new InvalidOperationException(
                "Intentional subscriber failure.");

        runtime.ScriptFaulted += faults.Add;

        runtime.Attach(brokenEntity, "test.broken");
        runtime.Attach(healthyEntity, "test.healthy");

        runtime.UpdateSystem.Update(default);

        Assert.Equal(1, healthyScript.UpdateCount);
        Assert.Single(faults);
        Assert.Equal(
            ScriptExecutionPhase.Update,
            faults[0].Phase);
    }


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
    public void AttachPersistsAssignmentAndPassesConfiguration()
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

        const string configuration =
            """{"speed":3,"enabled":true}""";

        runtime.Attach(
            entity,
            "test.recording",
            configuration);

        Assert.True(
            world.Has<ScriptAssignmentsComponent>(entity));

        var component =
            world.Get<ScriptAssignmentsComponent>(entity);

        var assignment =
            Assert.Single(component.Assignments.ToArray());

        Assert.Equal(
            "test.recording",
            assignment.ScriptId);

        Assert.Equal(
            configuration,
            assignment.ConfigurationJson);

        Assert.Equal(
            configuration,
            script.ConfigurationJson);
    }

    [Fact]
    public void DetachRemovesAssignmentComponentWhenEmpty()
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

        Assert.True(
            runtime.Detach(
                entity,
                "test.recording"));

        Assert.False(
            world.Has<ScriptAssignmentsComponent>(entity));
    }

    [Fact]
    public void DetachFailureIsReportedAndInstanceRemoved()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var registry = new ScriptRegistry();

        registry.Register<DetachFailingScript>(
            "test.detach-failing");

        using var runtime = new ScriptRuntime(
            world,
            registry);

        var faults = new List<ScriptFault>();

        runtime.ScriptFaulted += faults.Add;

        runtime.Attach(
            entity,
            "test.detach-failing");

        Assert.True(
            runtime.Detach(
                entity,
                "test.detach-failing"));

        Assert.Equal(0, runtime.InstanceCount);

        var fault = Assert.Single(faults);

        Assert.Equal(
            ScriptExecutionPhase.Detach,
            fault.Phase);

        Assert.Equal(
            "test.detach-failing",
            fault.ScriptId);
    }

    public sealed class AttachFailingScript : IEntityScript
    {
        public void OnAttach(ScriptContext context)
        {
            throw new InvalidOperationException(
                "Intentional attach failure.");
        }
    }

    public sealed class DetachFailingScript : IEntityScript
    {
        public void OnDetach(ScriptContext context)
        {
            throw new InvalidOperationException(
                "Intentional detach failure.");
        }
    }

    [Fact]
    public void RuntimeReconcilesScriptAssignmentsFromEcs()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var instances = new List<RecordingScript>();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.recording",
            () =>
            {
                var script = new RecordingScript();
                instances.Add(script);
                return script;
            });

        const string configuration =
            """{"speed":5}""";

        world.Add(
            entity,
            new ScriptAssignmentsComponent(
                new[]
                {
                new ScriptAssignment(
                    "test.recording",
                    configuration)
                }));

        using var runtime = new ScriptRuntime(
            world,
            registry);

        runtime.UpdateSystem.Update(default);

        var script = Assert.Single(instances);

        Assert.Equal(1, script.AttachCount);
        Assert.Equal(1, script.UpdateCount);
        Assert.Equal(configuration, script.ConfigurationJson);
        Assert.Equal(1, runtime.InstanceCount);

        Assert.True(
            world.Remove<ScriptAssignmentsComponent>(
                entity));

        runtime.UpdateSystem.Update(default);

        Assert.Equal(1, script.DetachCount);
        Assert.Equal(0, runtime.InstanceCount);
    }

    [Fact]
    public void AttachDefinitionUsesDefaultConfiguration()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var script = new RecordingScript();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.definition",
            () => script);

        using var runtime = new ScriptRuntime(
            world,
            registry);

        var definition = new ScriptDefinition(
            ScriptDefinition.CurrentFormatVersion,
            "test.definition",
            """{"speed":7}""");

        runtime.Attach(
            entity,
            definition);

        Assert.Equal(
            """{"speed":7}""",
            script.ConfigurationJson);

        Assert.True(
            world.Get<ScriptAssignmentsComponent>(entity)
                .Contains("test.definition"));

        Assert.Equal(
            1,
            runtime.InstanceCount);
    }


    [Fact]
    public void RebuildDuringExecutionIsRejectedWithoutBreakingRuntime()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var script = new RebuildDuringUpdateScript();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.rebuild-during-update",
            () => script);

        using var runtime = new ScriptRuntime(
            world,
            registry);

        script.Runtime = runtime;

        runtime.Attach(
            entity,
            "test.rebuild-during-update");

        runtime.UpdateSystem.Update(default);

        Assert.True(script.RebuildWasRejected);
        Assert.Equal(1, script.UpdateCount);
        Assert.Equal(0, script.DetachCount);
        Assert.Equal(1, runtime.InstanceCount);

        // После завершения Update runtime должен работать дальше.
        runtime.UpdateSystem.Update(default);

        Assert.Equal(2, script.UpdateCount);
        Assert.Equal(1, runtime.InstanceCount);

        Assert.True(
            world.Has<ScriptAssignmentsComponent>(entity));
    }

    [Fact]
    public void RuntimeRecreatesScriptWhenConfigurationChanges()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var instances = new List<RecordingScript>();
        var registry = new ScriptRegistry();

        registry.Register(
            "test.recording",
            () =>
            {
                var script = new RecordingScript();
                instances.Add(script);
                return script;
            });

        using var runtime = new ScriptRuntime(
            world,
            registry);

        const string initialConfiguration =
            """{"speed":1}""";

        const string updatedConfiguration =
            """{"speed":2}""";

        runtime.Attach(
            entity,
            "test.recording",
            initialConfiguration);

        var originalScript = Assert.Single(instances);

        // Имитируем изменение конфигурации через ECS.
        world.Add(
            entity,
            new ScriptAssignmentsComponent(
                new[]
                {
                new ScriptAssignment(
                    "test.recording",
                    updatedConfiguration)
                }));

        runtime.UpdateSystem.Update(default);

        Assert.Equal(2, instances.Count);

        Assert.Equal(1, originalScript.DetachCount);

        var updatedScript = instances[1];

        Assert.Equal(1, updatedScript.AttachCount);
        Assert.Equal(updatedConfiguration, updatedScript.ConfigurationJson);
        Assert.Equal(1, updatedScript.UpdateCount);

        Assert.Equal(1, runtime.InstanceCount);
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

    public sealed class RebuildDuringUpdateScript : IEntityScript
    {
        public ScriptRuntime Runtime { get; set; } = null!;

        public bool RebuildWasRejected { get; private set; }

        public int UpdateCount { get; private set; }

        public int DetachCount { get; private set; }

        public void Update(
            ScriptContext context,
            Engine.Core.Time.TimeSnapshot time)
        {
            UpdateCount++;

            try
            {
                Runtime.RebuildInstancesFromWorld();
            }
            catch (InvalidOperationException)
            {
                RebuildWasRejected = true;
            }
        }

        public void OnDetach(ScriptContext context)
        {
            DetachCount++;
        }
    }

    public sealed class RecordingScript : IEntityScript
    {
        public string? ConfigurationJson { get; private set; }
        public int AttachCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int FixedUpdateCount { get; private set; }

        public int DetachCount { get; private set; }

        public EntityId Entity { get; private set; }

        public void OnAttach(ScriptContext context)
        {
            AttachCount++;
            Entity = context.Entity;
            ConfigurationJson = context.ConfigurationJson;
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

    public sealed class BrokenFixedUpdateScript : IEntityScript
    {
        public void FixedUpdate(
            ScriptContext context,
            Engine.Core.Time.SimulationTime time)
        {
            throw new InvalidOperationException(
                "Intentional fixed update failure.");
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