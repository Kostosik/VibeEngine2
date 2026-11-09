
using Engine.AI.Behaviors;
using Engine.AI.Core;
using Engine.AI.Memory;
using Engine.AI.Perception.Core;
using Engine.AI.Perception.Sensors;
using Engine.AI.Systems;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Tests.AI;

public sealed class AILastKnownTargetTests
{
    [Fact]
    public void RemembersLastPositionAfterTargetLeavesSensorRange()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var observer = world.CreateEntity();
        world.Add(observer, new Engine.AI.Components.AIComponent());

        world.Add(
            observer,
            new TestPosition
            {
                Position = Vector2.Zero
            });

        var target = world.CreateEntity();

        world.Add(
            target,
            new TestPosition
            {
                Position = new Vector2(3f, 0f)
            });

        var behavior = new LastKnownTargetBehavior(target);

        ai.SetBehavior(observer, behavior);

        ai.Perception.RegisterSensor(
            observer,
            new RadiusSensor<TestPosition>(
                "Vision",
                10f,
                (in PerceptionContext context) =>
                    context.World
                        .GetReadOnly<TestPosition>(
                            context.Observer)
                        .Position,
                (entity, component) => component.Position));

        // First update: target is detected.
        AITestHelpers.Update(ai, 0.0, 0);

        Assert.NotNull(behavior.LastKnownTarget);

        Assert.Equal(
            new Vector2(3f, 0f),
            behavior.LastKnownTarget!.Value.Position);

        // Target leaves the sensor range.
        ref var targetPosition =
            ref world.Get<TestPosition>(target);

        targetPosition.Position = new Vector2(20f, 0f);

        AITestHelpers.Update(ai, 1.0, 1);

        // Previous position is still remembered.
        Assert.NotNull(behavior.LastKnownTarget);

        Assert.Equal(
            new Vector2(3f, 0f),
            behavior.LastKnownTarget!.Value.Position);

        // Default retention is 30 seconds.
        AITestHelpers.Update(ai, 31.0, 2);

        Assert.Null(behavior.LastKnownTarget);
    }

    private struct TestPosition
    {
        public Vector2 Position;
    }

    private sealed class LastKnownTargetBehavior : IAIBehavior
    {
        private readonly EntityId _target;

        public AILastKnownTarget? LastKnownTarget { get; private set; }

        public LastKnownTargetBehavior(EntityId target)
        {
            _target = target;
        }

        public void Update(in AIContext context)
        {
            LastKnownTarget =
                context.LastKnownTargets.TryGet(
                    _target,
                    context.Now,
                    out var remembered)
                    ? remembered
                    : null;
        }
    }
}
