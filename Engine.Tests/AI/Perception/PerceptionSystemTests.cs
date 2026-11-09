using Engine.AI.Components;
using Engine.AI.Perception;
using Engine.AI.Perception.Core;
using Engine.AI.Perception.Sensors;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Tests.AI.Perception;

public sealed class PerceptionSystemTests
{
    [Fact]
    public void RadiusSensor_DetectsEntitiesInsideRange()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, new Vector2(0, 0));
        var target = CreateTarget(world, new Vector2(3, 4));
        var outside = CreateTarget(world, new Vector2(11, 0));

        var sensor = CreateRadiusSensor(10f);

        perception.RegisterSensor(observer, sensor);

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        var observation = Assert.Single(state.Observations);

        Assert.Equal(target, observation.Target);
        Assert.Equal("Radius", observation.SensorName);

        Assert.Equal(25f, observation.DistanceSquared, 5);
        Assert.Equal(0.75f, observation.Confidence, 5);

        Assert.NotEqual(outside, observation.Target);
    }

    [Fact]
    public void RadiusSensor_DoesNotDetectObserverItself()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        perception.RegisterSensor(
            observer,
            CreateRadiusSensor(100f));

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        Assert.Empty(state.Observations);
    }

    [Fact]
    public void RadiusSensor_AppliesTargetFilter()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        var visible = CreateTarget(
            world,
            new Vector2(5, 0),
            detectable: true);

        var hidden = CreateTarget(
            world,
            new Vector2(6, 0),
            detectable: false);

        var sensor = new RadiusSensor<TestPosition>(
            "FilteredRadius",
            100f,
            (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<TestPosition>(
                        context.Observer)
                    .Position,
            (entity, component) => component.Position,
            (entity, component) => component.Detectable);

        perception.RegisterSensor(observer, sensor);

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        var observation = Assert.Single(state.Observations);

        Assert.Equal(visible, observation.Target);
        Assert.NotEqual(hidden, observation.Target);
    }

    [Fact]
    public void MultipleSensors_CanDetectSameTarget()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);
        var target = CreateTarget(world, new Vector2(5, 0));

        perception.RegisterSensor(
            observer,
            new StaticSensor("Visual", target, 0.7f));

        perception.RegisterSensor(
            observer,
            new StaticSensor("Radar", target, 0.9f));

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        Assert.Equal(2, state.Observations.Count);
        Assert.Equal(2, state.CountObservations(target));

        Assert.True(
            state.TryGetBestObservation(
                target,
                out var best));

        Assert.Equal("Radar", best.SensorName);
        Assert.Equal(0.9f, best.Confidence, 5);
    }

    [Fact]
    public void FailedSensor_DoesNotReplacePreviousObservations()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        var firstTarget = CreateTarget(
            world,
            new Vector2(1, 0));

        var secondTarget = CreateTarget(
            world,
            new Vector2(2, 0));

        var mutableSensor = new MutableSensor(
            "Mutable",
            firstTarget);

        var failingSensor = new FailingSensor();

        perception.RegisterSensor(observer, mutableSensor);
        perception.RegisterSensor(observer, failingSensor);

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        Assert.Equal(
            firstTarget,
            Assert.Single(state.Observations).Target);

        ulong previousVersion = state.Version;

        mutableSensor.Target = secondTarget;
        failingSensor.ShouldThrow = true;

        Assert.Throws<InvalidOperationException>(
            () => Update(perception, 0.1, 1));

        Assert.Equal(previousVersion, state.Version);

        Assert.Equal(
            firstTarget,
            Assert.Single(state.Observations).Target);
    }

    [Fact]
    public void DisabledAgent_DoesNotUpdatePerception()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        var target = CreateTarget(
            world,
            new Vector2(1, 0));

        var sensor = new StaticSensor(
            "Static",
            target,
            1f);

        perception.RegisterSensor(observer, sensor);

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        ulong version = state.Version;

        ref var ai = ref world.Get<AIComponent>(observer);

        ai.Enabled = false;

        Update(perception, 0.1, 1);

        Assert.Equal(version, state.Version);
    }

    [Fact]
    public void RemovingLastSensor_RemovesAgentState()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        var sensor = new StaticSensor(
            "Static",
            new EntityId(100, 1),
            1f);

        perception.RegisterSensor(observer, sensor);

        Assert.Equal(1, perception.AgentCount);

        Assert.True(
            perception.RemoveSensor(observer, sensor));

        Assert.Equal(0, perception.AgentCount);
        Assert.False(perception.TryGetState(observer, out _));
    }

    [Fact]
    public void DestroyedObserver_RemovesPerceptionState()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        perception.RegisterSensor(
            observer,
            new StaticSensor(
                "Static",
                new EntityId(100, 1),
                1f));

        Update(perception, 0.0, 0);

        Assert.True(
            perception.TryGetState(observer, out _));

        Assert.True(world.DestroyEntity(observer));

        Assert.False(
            perception.TryGetState(observer, out _));

        Assert.Equal(0, perception.AgentCount);
    }

    [Fact]
    public void RegisterSensor_RejectsEntityWithoutAIComponent()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var entity = world.CreateEntity();

        Assert.Throws<InvalidOperationException>(
            () => perception.RegisterSensor(
                entity,
                new StaticSensor(
                    "Static",
                    new EntityId(100, 1),
                    1f)));
    }

    [Fact]
    public void RegisterSensor_RejectsDuplicateSensorName()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        perception.RegisterSensor(
            observer,
            new StaticSensor(
                "Radar",
                new EntityId(100, 1),
                1f));

        Assert.Throws<InvalidOperationException>(
            () => perception.RegisterSensor(
                observer,
                new StaticSensor(
                    "Radar",
                    new EntityId(101, 1),
                    1f)));
    }

    [Fact]
    public void RadiusSensor_RejectsInvalidRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateRadiusSensor(0f));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateRadiusSensor(-10f));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateRadiusSensor(float.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateRadiusSensor(float.PositiveInfinity));
    }

    private static EntityId CreateAgent(
        World world,
        Vector2 position)
    {
        var entity = world.CreateEntity();

        world.Add(entity, new AIComponent());

        world.Add(
            entity,
            new TestPosition
            {
                Position = position,
                Detectable = true
            });

        return entity;
    }

    private static EntityId CreateTarget(
        World world,
        Vector2 position,
        bool detectable = true)
    {
        var entity = world.CreateEntity();

        world.Add(
            entity,
            new TestPosition
            {
                Position = position,
                Detectable = detectable
            });

        return entity;
    }

    private static RadiusSensor<TestPosition> CreateRadiusSensor(
        float range)
    {
        return new RadiusSensor<TestPosition>(
            "Radius",
            range,
            (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<TestPosition>(
                        context.Observer)
                    .Position,
            (entity, component) => component.Position);
    }

    private static PerceptionState GetState(
        PerceptionSystem system,
        EntityId entity)
    {
        Assert.True(
            system.TryGetState(
                entity,
                out var state));

        return state!;
    }

    private static void Update(
        PerceptionSystem system,
        double elapsedSeconds,
        ulong tick)
    {
        system.Update(
            new SystemContext(
                new TimeSnapshot(
                    Duration.FromMilliseconds(16),
                    Duration.FromSeconds(elapsedSeconds),
                    new Tick(tick))));
    }

    private struct TestPosition
    {
        public Vector2 Position;

        public bool Detectable;
    }

    private sealed class StaticSensor : IPerceptionSensor
    {
        private readonly EntityId _target;

        private readonly float _confidence;

        public string Name { get; }

        public StaticSensor(
            string name,
            EntityId target,
            float confidence)
        {
            Name = name;
            _target = target;
            _confidence = confidence;
        }

        public void Sense(
            in PerceptionContext context,
            PerceptionBuffer results)
        {
            results.Add(
                _target,
                _confidence);
        }
    }

    private sealed class MutableSensor : IPerceptionSensor
    {
        public string Name { get; }

        public EntityId Target { get; set; }

        public MutableSensor(
            string name,
            EntityId target)
        {
            Name = name;
            Target = target;
        }

        public void Sense(
            in PerceptionContext context,
            PerceptionBuffer results)
        {
            results.Add(Target);
        }
    }


    [Fact]
    public void FieldOfViewSensor_DetectsOnlyTargetsInsideCone()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        var front = CreateTarget(
            world,
            new Vector2(5f, 0f));

        var side = CreateTarget(
            world,
            new Vector2(0f, 5f));

        var behind = CreateTarget(
            world,
            new Vector2(-5f, 0f));

        var sensor = new FieldOfViewSensor<TestPosition>(
            "Vision",
            10f,
            90f,
            (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<TestPosition>(
                        context.Observer)
                    .Position,
            (in PerceptionContext context) =>
                new Vector2(1f, 0f),
            (entity, component) => component.Position);

        perception.RegisterSensor(observer, sensor);

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        var observation = Assert.Single(state.Observations);

        Assert.Equal(front, observation.Target);
        Assert.NotEqual(side, observation.Target);
        Assert.NotEqual(behind, observation.Target);
    }


    [Fact]
    public void RadiusSensor_DoesNotDetectTargetsBehindObstacles()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        var visibleTarget = CreateTarget(
            world,
            new Vector2(3, 0));

        var blockedTarget = CreateTarget(
            world,
            new Vector2(6, 0));

        var sensor = new RadiusSensor<TestPosition>(
            "OcclusionRadius",
            10f,
            (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<TestPosition>(
                        context.Observer)
                    .Position,
            (entity, component) => component.Position,
            visibilityTest: IsVisible);

        perception.RegisterSensor(observer, sensor);

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        var observation = Assert.Single(state.Observations);

        Assert.Equal(visibleTarget, observation.Target);
        Assert.NotEqual(blockedTarget, observation.Target);

        static bool IsVisible(
            in PerceptionContext context,
            EntityId target,
            TestPosition component,
            Vector2 observerPosition,
            Vector2 targetPosition)
        {
            // Временно считаем всё правее X = 5 закрытым препятствием.
            return targetPosition.X < 5f;
        }
    }


    [Fact]
    public void SensorInterval_SkipsUpdatesUntilIntervalExpires()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);
        var target = CreateTarget(world, new Vector2(2, 0));

        var sensor = new CountingSensor("SlowSensor", target);

        perception.RegisterSensor(
            observer,
            sensor,
            new PerceptionSensorSettings(
                Duration.FromMilliseconds(100)));

        Update(perception, 0.0, 0);

        var state = GetState(perception, observer);

        Assert.Equal(1, sensor.UpdateCount);
        Assert.Equal(1UL, state.Version);

        Update(perception, 0.05, 1);

        Assert.Equal(1, sensor.UpdateCount);
        Assert.Equal(1UL, state.Version);

        // The previous observation remains available.
        Assert.Equal(
            target,
            Assert.Single(state.Observations).Target);

        Update(perception, 0.10, 2);

        Assert.Equal(2, sensor.UpdateCount);
        Assert.Equal(2UL, state.Version);
    }

    [Fact]
    public void SensorBudget_DistributesUpdatesWithoutStarvation()
    {
        using var world = new World();

        using var perception = new PerceptionSystem(
            world,
            new PerceptionSystemOptions
            {
                MaxSensorUpdatesPerUpdate = 1
            });

        var observerA = CreateAgent(world, Vector2.Zero);
        var observerB = CreateAgent(world, Vector2.Zero);

        var targetA = CreateTarget(world, new Vector2(2, 0));
        var targetB = CreateTarget(world, new Vector2(3, 0));

        var sensorA = new CountingSensor("SensorA", targetA);
        var sensorB = new CountingSensor("SensorB", targetB);

        perception.RegisterSensor(observerA, sensorA);
        perception.RegisterSensor(observerB, sensorB);

        Update(perception, 0.0, 0);

        Assert.Equal(
            1,
            sensorA.UpdateCount + sensorB.UpdateCount);

        Update(perception, 0.016, 1);

        Assert.Equal(1, sensorA.UpdateCount);
        Assert.Equal(1, sensorB.UpdateCount);

        Assert.Equal(
            2,
            sensorA.UpdateCount + sensorB.UpdateCount);
    }


    [Fact]
    public void FieldOfViewSensor_FullCircleDetectsTargetsBehindObserver()
    {
        using var world = new World();
        using var perception = new PerceptionSystem(world);

        var observer = CreateAgent(world, Vector2.Zero);

        var target = CreateTarget(
            world,
            new Vector2(-5f, 0f));

        var sensor = new FieldOfViewSensor<TestPosition>(
            "Omnidirectional",
            10f,
            360f,
            (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<TestPosition>(
                        context.Observer)
                    .Position,
            (in PerceptionContext context) =>
                new Vector2(1f, 0f),
            (entity, component) => component.Position);

        perception.RegisterSensor(observer, sensor);

        Update(perception, 0.0, 0);

        var observation = Assert.Single(
            GetState(perception, observer).Observations);

        Assert.Equal(target, observation.Target);
    }


    private sealed class CountingSensor : IPerceptionSensor
    {
        private readonly EntityId _target;

        public string Name { get; }

        public int UpdateCount { get; private set; }

        public CountingSensor(
            string name,
            EntityId target)
        {
            Name = name;
            _target = target;
        }

        public void Sense(
            in PerceptionContext context,
            PerceptionBuffer results)
        {
            UpdateCount++;

            results.Add(_target);
        }
    }


    private sealed class FailingSensor : IPerceptionSensor
    {
        public string Name => "Failing";

        public bool ShouldThrow { get; set; }

        public void Sense(
            in PerceptionContext context,
            PerceptionBuffer results)
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException(
                    "Sensor failure.");
            }
        }
    }
}