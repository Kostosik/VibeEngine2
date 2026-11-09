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

public sealed class AISystemTests
{
    [Fact]
    public void DestroyedTarget_IsRemovedFromAllAgentsLastKnownMemory()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var firstObserver = AITestHelpers.CreateAgent(world);
        var secondObserver = AITestHelpers.CreateAgent(world);
        var target = world.CreateEntity();

        world.Add(
            firstObserver,
            new TargetPositionComponent
            {
                Position = new Vector2(0f, 0f)
            });

        world.Add(
            secondObserver,
            new TargetPositionComponent
            {
                Position = new Vector2(1f, 0f)
            });

        world.Add(
            target,
            new TargetPositionComponent
            {
                Position = new Vector2(3f, 0f)
            });

        var firstBehavior = new LastKnownTargetBehavior(target);
        var secondBehavior = new LastKnownTargetBehavior(target);

        ai.SetBehavior(firstObserver, firstBehavior);
        ai.SetBehavior(secondObserver, secondBehavior);

        ai.Perception.RegisterSensor(
            firstObserver,
            CreateVisionSensor());

        ai.Perception.RegisterSensor(
            secondObserver,
            CreateVisionSensor());

        AITestHelpers.Update(ai, 0.0);

        Assert.NotNull(firstBehavior.RememberedTarget);
        Assert.NotNull(secondBehavior.RememberedTarget);

        Assert.Equal(
            target,
            firstBehavior.RememberedTarget!.Value.Target);

        Assert.Equal(
            target,
            secondBehavior.RememberedTarget!.Value.Target);

        Assert.True(world.DestroyEntity(target));

        AITestHelpers.Update(ai, 1.0);

        Assert.Null(firstBehavior.RememberedTarget);
        Assert.Null(secondBehavior.RememberedTarget);
    }

    [Fact]
    public void UpdatesOnlyEnabledAgents()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var first = AITestHelpers.CreateAgent(world);
        var second = AITestHelpers.CreateAgent(world);

        ref var secondComponent =
            ref world.Get<Engine.AI.Components.AIComponent>(
                second);

        secondComponent.Enabled = false;

        var firstBehavior = new CounterBehavior();
        var secondBehavior = new CounterBehavior();

        ai.SetBehavior(first, firstBehavior);
        ai.SetBehavior(second, secondBehavior);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(1, firstBehavior.UpdateCount);
        Assert.Equal(0, secondBehavior.UpdateCount);
    }

    [Fact]
    public void SetBehavior_RejectsEntityWithoutAIComponent()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = world.CreateEntity();

        Assert.Throws<InvalidOperationException>(
            () => ai.SetBehavior(
                entity,
                new CounterBehavior()));
    }

    [Fact]
    public void RemoveBehavior_InvokesLifecycle()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var behavior = new LifecycleBehavior();

        ai.SetBehavior(entity, behavior);

        Assert.True(ai.RemoveBehavior(entity));

        Assert.Equal(1, behavior.DetachCount);
        Assert.Equal(0, ai.AgentCount);
    }

    [Fact]
    public void ReplacingBehavior_InvokesPreviousLifecycle()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var previous = new LifecycleBehavior();
        var replacement = new CounterBehavior();

        ai.SetBehavior(entity, previous);
        ai.SetBehavior(entity, replacement);

        Assert.Equal(1, previous.DetachCount);

        Assert.True(
            ai.TryGetBehavior(entity, out var actual));

        Assert.Same(replacement, actual);
    }

    [Fact]
    public void RemovingAIComponent_RemovesBehaviorOnNextUpdate()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        ai.SetBehavior(entity, new CounterBehavior());

        Assert.Equal(1, ai.AgentCount);

        Assert.True(
            world.Remove<Engine.AI.Components.AIComponent>(
                entity));

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(0, ai.AgentCount);

        Assert.False(
            ai.TryGetBehavior(entity, out _));
    }


    [Fact]
    public void AgentMemory_SurvivesBehaviorReplacement()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var writer = new MemoryWriterBehavior();

        ai.SetBehavior(entity, writer);

        AITestHelpers.Update(ai, 0.0);

        var reader = new MemoryReaderBehavior();

        ai.SetBehavior(entity, reader);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(42, reader.ReadValue);
    }


    private sealed class CounterBehavior : IAIBehavior
    {
        public int UpdateCount { get; private set; }

        public void Update(in AIContext context)
        {
            UpdateCount++;
        }
    }

    private sealed class LifecycleBehavior :
        IAIBehavior,
        IAIBehaviorLifecycle
    {
        public int DetachCount { get; private set; }

        public void Update(in AIContext context)
        {
        }

        public void OnDetached(AIAgentState agentState)
        {
            DetachCount++;
        }
    }

    [Fact]
    public void LastKnownTarget_RefreshesWhenTargetIsDetectedAgain()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var observer = AITestHelpers.CreateAgent(world);
        var target = world.CreateEntity();

        world.Add(
            observer,
            new TargetPositionComponent
            {
                Position = new Vector2(0f, 0f)
            });

        world.Add(
            target,
            new TargetPositionComponent
            {
                Position = new Vector2(3f, 0f)
            });

        var behavior = new LastKnownTargetBehavior(target);

        ai.SetBehavior(observer, behavior);
        ai.Perception.RegisterSensor(
            observer,
            CreateVisionSensor());

        AITestHelpers.Update(ai, 0.0);

        Assert.NotNull(behavior.RememberedTarget);
        Assert.Equal(
            new Vector2(3f, 0f),
            behavior.RememberedTarget!.Value.Position);

        // Теряем цель.
        ref var targetPosition =
            ref world.Get<TargetPositionComponent>(target);

        targetPosition.Position = new Vector2(20f, 0f);

        AITestHelpers.Update(ai, 1.0);

        Assert.NotNull(behavior.RememberedTarget);
        Assert.Equal(
            new Vector2(3f, 0f),
            behavior.RememberedTarget!.Value.Position);

        // Цель снова появляется в поле зрения в новой позиции.
        targetPosition.Position = new Vector2(4f, 0f);

        AITestHelpers.Update(ai, 2.0);

        Assert.NotNull(behavior.RememberedTarget);
        Assert.Equal(
            new Vector2(4f, 0f),
            behavior.RememberedTarget!.Value.Position);

        Assert.Equal(
            TimeSpan.FromSeconds(2),
            behavior.RememberedTarget.Value.LastSeenAt);
    }

    [Fact]
    public void LastKnownTarget_ExpiresAfterRetentionPeriod()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var observer = AITestHelpers.CreateAgent(world);
        var target = world.CreateEntity();

        world.Add(
            observer,
            new TargetPositionComponent
            {
                Position = new Vector2(0f, 0f)
            });

        world.Add(
            target,
            new TargetPositionComponent
            {
                Position = new Vector2(3f, 0f)
            });

        var behavior = new LastKnownTargetBehavior(target);

        ai.SetBehavior(observer, behavior);
        ai.Perception.RegisterSensor(
            observer,
            CreateVisionSensor());

        AITestHelpers.Update(ai, 0.0);

        Assert.NotNull(behavior.RememberedTarget);

        // Цель покидает радиус обнаружения.
        ref var targetPosition =
            ref world.Get<TargetPositionComponent>(target);

        targetPosition.Position = new Vector2(20f, 0f);

        AITestHelpers.Update(ai, 1.0);

        Assert.NotNull(behavior.RememberedTarget);

        // Ещё не истекли 30 секунд.
        AITestHelpers.Update(ai, 29.999);

        Assert.NotNull(behavior.RememberedTarget);

        // Ровно 30 секунд с момента последнего обнаружения.
        AITestHelpers.Update(ai, 30.0);

        Assert.Null(behavior.RememberedTarget);
    }

    [Fact]
    public void LastKnownTargets_RemembersPositionAfterTargetLeavesVision()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var observer = AITestHelpers.CreateAgent(world);
        var target = world.CreateEntity();

        world.Add(
            observer,
            new TestPosition
            {
                Position = Vector2.Zero
            });

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

        // Target is visible.
        AITestHelpers.Update(ai, 0.0);

        Assert.NotNull(behavior.RememberedTarget);

        Assert.Equal(
            target,
            behavior.RememberedTarget!.Value.Target);

        Assert.Equal(
            new Vector2(3f, 0f),
            behavior.RememberedTarget.Value.Position);

        // Target leaves sensor range.
        ref var position = ref world.Get<TestPosition>(target);
        position.Position = new Vector2(20f, 0f);

        AITestHelpers.Update(ai, 1.0);

        // The old position must remain available.
        Assert.NotNull(behavior.RememberedTarget);

        Assert.Equal(
            new Vector2(3f, 0f),
            behavior.RememberedTarget!.Value.Position);
    }

    private struct TestPosition
    {
        public Vector2 Position;
    }

    private sealed class LastKnownTargetBehavior : IAIBehavior
    {
        private readonly EntityId _target;

        public AILastKnownTarget? RememberedTarget { get; private set; }

        public LastKnownTargetBehavior(EntityId target)
        {
            _target = target;
        }

        public void Update(in AIContext context)
        {
            RememberedTarget =
                context.LastKnownTargets.TryGet(
                    _target,
                    context.Now,
                    out var remembered)
                    ? remembered
                    : null;
        }
    }



    private static readonly AIMemoryKey<int> SharedValueKey =
        new("SharedValue");

    private sealed class MemoryWriterBehavior : IAIBehavior
    {
        public void Update(in AIContext context)
        {
            context.Memory.Set(
                SharedValueKey,
                42,
                context.Now);
        }
    }

    private sealed class MemoryReaderBehavior : IAIBehavior
    {
        public int? ReadValue { get; private set; }

        public void Update(in AIContext context)
        {
            if (context.Memory.TryGet(
                    SharedValueKey,
                    context.Now,
                    out var value))
            {
                ReadValue = value;
            }
        }
    }
    private static RadiusSensor<TargetPositionComponent> CreateVisionSensor()
    {
        return new RadiusSensor<TargetPositionComponent>(
            "Vision",
            10f,
            static (in PerceptionContext context) =>
                context.World
                    .GetReadOnly<TargetPositionComponent>(
                        context.Observer)
                    .Position,
            static (_, component) => component.Position);
    }

    private struct TargetPositionComponent
    {
        public Vector2 Position { get; set; }
    }
}