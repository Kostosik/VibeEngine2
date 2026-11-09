using Engine.AI.Behaviors;
using Engine.AI.Components;
using Engine.AI.Core;
using Engine.AI.Scheduling;
using Engine.AI.Systems;
using Engine.ECS;

namespace Engine.Tests.AI.Scheduling;

public sealed class AISystemSchedulingTests
{
    [Fact]
    public void AISystem_RespectsUpdateBudget()
    {
        using var world = new World();

        using var ai = new AISystem(
            world,
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 1
            });

        var first = AITestHelpers.CreateAgent(world);
        var second = AITestHelpers.CreateAgent(world);
        var third = AITestHelpers.CreateAgent(world);

        var firstBehavior = new CounterBehavior();
        var secondBehavior = new CounterBehavior();
        var thirdBehavior = new CounterBehavior();

        ai.SetBehavior(first, firstBehavior);
        ai.SetBehavior(second, secondBehavior);
        ai.SetBehavior(third, thirdBehavior);

        AITestHelpers.Update(ai, 0.0, 0);

        Assert.Equal(1, firstBehavior.UpdateCount);
        Assert.Equal(0, secondBehavior.UpdateCount);
        Assert.Equal(0, thirdBehavior.UpdateCount);

        AITestHelpers.Update(ai, 0.0, 1);

        Assert.Equal(1, firstBehavior.UpdateCount);
        Assert.Equal(1, secondBehavior.UpdateCount);
        Assert.Equal(0, thirdBehavior.UpdateCount);

        AITestHelpers.Update(ai, 0.0, 2);

        Assert.Equal(1, firstBehavior.UpdateCount);
        Assert.Equal(1, secondBehavior.UpdateCount);
        Assert.Equal(1, thirdBehavior.UpdateCount);
    }

    [Fact]
    public void AISystem_DisabledAgentDoesNotConsumeBudget()
    {
        using var world = new World();

        using var ai = new AISystem(
            world,
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 1
            });

        var disabled = AITestHelpers.CreateAgent(world);
        var enabled = AITestHelpers.CreateAgent(world);

        ref var disabledComponent =
            ref world.Get<AIComponent>(disabled);

        disabledComponent.Enabled = false;

        var disabledBehavior = new CounterBehavior();
        var enabledBehavior = new CounterBehavior();

        ai.SetBehavior(disabled, disabledBehavior);
        ai.SetBehavior(enabled, enabledBehavior);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(0, disabledBehavior.UpdateCount);
        Assert.Equal(1, enabledBehavior.UpdateCount);
    }

    [Fact]
    public void AISystem_UsesCustomAgentInterval()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        world.Add(
            entity,
            new AIUpdateSettings(
                AIUpdateGroup.Normal,
                updateInterval:
                    Engine.Core.Time.Duration.FromSeconds(2)));

        var behavior = new CounterBehavior();

        ai.SetBehavior(entity, behavior);

        AITestHelpers.Update(ai, 0.0, 0);

        Assert.Equal(1, behavior.UpdateCount);

        AITestHelpers.Update(ai, 1.0, 1);

        Assert.Equal(1, behavior.UpdateCount);

        AITestHelpers.Update(ai, 2.0, 2);

        Assert.Equal(2, behavior.UpdateCount);
    }

    [Fact]
    public void AISystem_RespectsPriority()
    {
        using var world = new World();

        using var ai = new AISystem(
            world,
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 1
            });

        var low = AITestHelpers.CreateAgent(world);
        var high = AITestHelpers.CreateAgent(world);

        world.Add(
            low,
            new AIUpdateSettings(
                AIUpdateGroup.EveryUpdate,
                priority: 0));

        world.Add(
            high,
            new AIUpdateSettings(
                AIUpdateGroup.EveryUpdate,
                priority: 100));

        var lowBehavior = new CounterBehavior();
        var highBehavior = new CounterBehavior();

        ai.SetBehavior(low, lowBehavior);
        ai.SetBehavior(high, highBehavior);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(0, lowBehavior.UpdateCount);
        Assert.Equal(1, highBehavior.UpdateCount);
    }

    [Fact]
    public void AISystem_RemovingBehaviorUnregistersAgent()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var behavior = new CounterBehavior();

        ai.SetBehavior(entity, behavior);

        Assert.True(ai.Scheduler.IsRegistered(entity));

        Assert.True(ai.RemoveBehavior(entity));

        Assert.False(ai.Scheduler.IsRegistered(entity));
        Assert.Equal(0, ai.Scheduler.AgentCount);
    }

    [Fact]
    public void AISystem_DestroyingEntityUnregistersAgent()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        ai.SetBehavior(entity, new CounterBehavior());

        Assert.True(world.DestroyEntity(entity));

        Assert.False(ai.Scheduler.IsRegistered(entity));
        Assert.Equal(0, ai.AgentCount);
    }

    private sealed class CounterBehavior : IAIBehavior
    {
        public int UpdateCount { get; private set; }

        public void Update(in AIContext context)
        {
            UpdateCount++;
        }
    }
}