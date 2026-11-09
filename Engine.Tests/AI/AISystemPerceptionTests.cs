
using Engine.AI.Behaviors;
using Engine.AI.Components;
using Engine.AI.Core;
using Engine.AI.Perception.Core;
using Engine.AI.Perception.Sensors;
using Engine.AI.Systems;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Tests.AI;

public sealed class AISystemPerceptionTests
{
    [Fact]
    public void AISystem_UpdatesPerceptionBeforeBehavior()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var observer = AITestHelpers.CreateAgent(world);

        world.Add(
            observer,
            new TestPosition { Position = Vector2.Zero });

        var target = world.CreateEntity();

        world.Add(
            target,
            new TestPosition
            {
                Position = new Vector2(5f, 0f)
            });

        var sensor = new RadiusSensor<TestPosition>(
            "Sight",
            10f,
            (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<TestPosition>(
                        context.Observer)
                    .Position,
            (entity, component) => component.Position);

        ai.Perception.RegisterSensor(observer, sensor);

        var behavior = new PerceptionCaptureBehavior(target);

        ai.SetBehavior(observer, behavior);

        AITestHelpers.Update(ai, 0.0);

        Assert.True(behavior.TargetDetected);
    }

    private struct TestPosition
    {
        public Vector2 Position;
    }

    private sealed class PerceptionCaptureBehavior : IAIBehavior
    {
        private readonly EntityId _target;

        public bool TargetDetected { get; private set; }

        public PerceptionCaptureBehavior(EntityId target)
        {
            _target = target;
        }

        public void Update(in AIContext context)
        {
            TargetDetected =
                context.Perception is { } perception &&
                perception.TryGetBestObservation(
                    _target,
                    out _);
        }
    }
}
