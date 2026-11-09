using Engine.AI.Behaviors;
using Engine.AI.Core;
using Engine.AI.Memory;
using Engine.AI.Systems;
using Engine.ECS;

namespace Engine.Tests.AI;

public sealed class AISystemTests
{
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

}