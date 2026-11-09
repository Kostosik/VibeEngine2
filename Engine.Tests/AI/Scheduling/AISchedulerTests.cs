using Engine.AI.Components;
using Engine.AI.Scheduling;
using Engine.AI.Systems;
using Engine.AI.Core;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Tests.AI.Scheduling;

public sealed class AISchedulerTests
{
    [Fact]
    public void RegisterAgent_IsImmediatelyEligible()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        scheduler.RegisterOrUpdate(
            entity,
            new AIUpdateSettings(),
            TimeSpan.Zero);

        var due = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            new[] { entity });

        Assert.Contains(entity, due);
    }

    [Fact]
    public void EveryUpdate_GroupRunsOnEverySchedulerUpdate()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        scheduler.RegisterOrUpdate(
            entity,
            new AIUpdateSettings(AIUpdateGroup.EveryUpdate),
            TimeSpan.Zero);

        for (int i = 0; i < 3; i++)
        {
            var due = scheduler.CollectDueAgents(
                TimeSpan.FromMilliseconds(i * 16),
                new[] { entity });

            Assert.Contains(entity, due);

            scheduler.MarkUpdated(
                entity,
                TimeSpan.FromMilliseconds(i * 16));
        }
    }

    [Fact]
    public void NormalGroup_RespectsUpdateInterval()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        scheduler.RegisterOrUpdate(
            entity,
            new AIUpdateSettings(AIUpdateGroup.Normal),
            TimeSpan.Zero);

        scheduler.MarkUpdated(entity, TimeSpan.Zero);

        Assert.Empty(
            scheduler.CollectDueAgents(
                TimeSpan.FromMilliseconds(100),
                new[] { entity }));

        Assert.Contains(
            entity,
            scheduler.CollectDueAgents(
                TimeSpan.FromMilliseconds(250),
                new[] { entity }));
    }

    [Fact]
    public void CustomInterval_OverridesGroupInterval()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        var settings = new AIUpdateSettings(
            AIUpdateGroup.Background,
            priority: 0,
            updateInterval: Duration.FromMilliseconds(100));

        scheduler.RegisterOrUpdate(
            entity,
            settings,
            TimeSpan.Zero);

        scheduler.MarkUpdated(entity, TimeSpan.Zero);

        Assert.Empty(
            scheduler.CollectDueAgents(
                TimeSpan.FromMilliseconds(50),
                new[] { entity }));

        Assert.Contains(
            entity,
            scheduler.CollectDueAgents(
                TimeSpan.FromMilliseconds(100),
                new[] { entity }));
    }

    [Fact]
    public void Priority_HigherPriorityRunsFirst()
    {
        var scheduler = new AIScheduler(
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 1
            });

        var low = new EntityId(1, 1);
        var high = new EntityId(2, 1);

        scheduler.RegisterOrUpdate(
            low,
            new AIUpdateSettings(
                AIUpdateGroup.EveryUpdate,
                priority: 10),
            TimeSpan.Zero);

        scheduler.RegisterOrUpdate(
            high,
            new AIUpdateSettings(
                AIUpdateGroup.EveryUpdate,
                priority: 100),
            TimeSpan.Zero);

        var due = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            new[] { low, high });

        Assert.Equal(high, Assert.Single(due));
    }

    [Fact]
    public void Budget_DoesNotExceedMaximumAgentCount()
    {
        var scheduler = new AIScheduler(
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 2
            });

        var entities = Enumerable.Range(1, 10)
            .Select(i => new EntityId((uint)i, 1))
            .ToArray();

        foreach (var entity in entities)
        {
            scheduler.RegisterOrUpdate(
                entity,
                new AIUpdateSettings(),
                TimeSpan.Zero);
        }

        var due = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            entities);

        Assert.Equal(2, due.Count);
    }

    [Fact]
    public void EqualPriority_UsesRoundRobinOrder()
    {
        var scheduler = new AIScheduler(
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 1
            });

        var first = new EntityId(1, 1);
        var second = new EntityId(2, 1);
        var third = new EntityId(3, 1);

        var entities = new[] { first, second, third };

        foreach (var entity in entities)
        {
            scheduler.RegisterOrUpdate(
                entity,
                new AIUpdateSettings(),
                TimeSpan.Zero);
        }

        var firstDue = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            entities);

        Assert.Equal(first, Assert.Single(firstDue));

        scheduler.MarkUpdated(first, TimeSpan.Zero);

        var secondDue = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            entities);

        Assert.Equal(second, Assert.Single(secondDue));

        scheduler.MarkUpdated(second, TimeSpan.Zero);

        var thirdDue = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            entities);

        Assert.Equal(third, Assert.Single(thirdDue));

        scheduler.MarkUpdated(third, TimeSpan.Zero);

        var nextDue = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            entities);

        Assert.Equal(first, Assert.Single(nextDue));
    }

    [Fact]
    public void PriorityAging_PreventsStarvation()
    {
        var scheduler = new AIScheduler(
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 1,
                PriorityAgingInterval =
                    Duration.FromMilliseconds(100)
            });

        var high = new EntityId(1, 1);
        var low = new EntityId(2, 1);

        scheduler.RegisterOrUpdate(
            high,
            new AIUpdateSettings(
                AIUpdateGroup.EveryUpdate,
                priority: 1),
            TimeSpan.Zero);

        scheduler.RegisterOrUpdate(
            low,
            new AIUpdateSettings(
                AIUpdateGroup.EveryUpdate,
                priority: 0),
            TimeSpan.Zero);

        var first = scheduler.CollectDueAgents(
            TimeSpan.Zero,
            new[] { high, low });

        Assert.Equal(high, Assert.Single(first));

        scheduler.MarkUpdated(high, TimeSpan.Zero);

        var second = scheduler.CollectDueAgents(
            TimeSpan.FromMilliseconds(100),
            new[] { high, low });

        Assert.Equal(high, Assert.Single(second));

        scheduler.MarkUpdated(
            high,
            TimeSpan.FromMilliseconds(100));

        var third = scheduler.CollectDueAgents(
            TimeSpan.FromMilliseconds(200),
            new[] { high, low });

        Assert.Equal(low, Assert.Single(third));
    }

    [Fact]
    public void UpdateSettings_ReschedulesAgentImmediately()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        scheduler.RegisterOrUpdate(
            entity,
            new AIUpdateSettings(AIUpdateGroup.Background),
            TimeSpan.Zero);

        scheduler.MarkUpdated(entity, TimeSpan.Zero);

        Assert.Empty(
            scheduler.CollectDueAgents(
                TimeSpan.FromMilliseconds(100),
                new[] { entity }));

        bool changed = scheduler.RegisterOrUpdate(
            entity,
            new AIUpdateSettings(AIUpdateGroup.High),
            TimeSpan.FromMilliseconds(100));

        Assert.True(changed);

        Assert.Contains(
            entity,
            scheduler.CollectDueAgents(
                TimeSpan.FromMilliseconds(100),
                new[] { entity }));
    }

    [Fact]
    public void Unregister_RemovesAgentFromScheduler()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        scheduler.RegisterOrUpdate(
            entity,
            new AIUpdateSettings(),
            TimeSpan.Zero);

        Assert.True(scheduler.Unregister(entity));

        Assert.False(scheduler.IsRegistered(entity));
        Assert.Equal(0, scheduler.AgentCount);
    }

    [Fact]
    public void StaggeredUpdates_UseStableOffsets()
    {
        var firstScheduler = new AIScheduler(
            new AISchedulerOptions
            {
                StaggerInitialUpdates = true
            });

        var secondScheduler = new AIScheduler(
            new AISchedulerOptions
            {
                StaggerInitialUpdates = true
            });

        var entity = new EntityId(123, 5);

        var settings = new AIUpdateSettings(
            AIUpdateGroup.Background);

        firstScheduler.RegisterOrUpdate(
            entity,
            settings,
            TimeSpan.Zero);

        secondScheduler.RegisterOrUpdate(
            entity,
            settings,
            TimeSpan.Zero);

        Assert.True(
            firstScheduler.TryGetNextUpdateTime(
                entity,
                out var firstTime));

        Assert.True(
            secondScheduler.TryGetNextUpdateTime(
                entity,
                out var secondTime));

        Assert.Equal(firstTime, secondTime);

        Assert.InRange(
            firstTime,
            TimeSpan.Zero,
            Duration.FromSeconds(1).Value);
    }

    [Fact]
    public void NegativeCustomInterval_Throws()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        var settings = new AIUpdateSettings(
            AIUpdateGroup.Normal,
            updateInterval: Duration.FromSeconds(-1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => scheduler.RegisterOrUpdate(
                entity,
                settings,
                TimeSpan.Zero));
    }

    [Fact]
    public void UnknownUpdateGroup_Throws()
    {
        var scheduler = new AIScheduler();

        var entity = new EntityId(1, 1);

        var settings = new AIUpdateSettings(
            (AIUpdateGroup)255);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => scheduler.RegisterOrUpdate(
                entity,
                settings,
                TimeSpan.Zero));
    }

    [Fact]
    public void MaxAgentsPerUpdate_MustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AISchedulerOptions
            {
                MaxAgentsPerUpdate = 0
            });
    }

    [Fact]
    public void ZeroPriorityAgingInterval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AISchedulerOptions
            {
                PriorityAgingInterval = Duration.Zero
            });
    }
}