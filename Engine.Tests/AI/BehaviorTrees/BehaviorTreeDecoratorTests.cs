using Engine.AI.BehaviorTrees;
using Engine.AI.BehaviorTrees.Core;
using Engine.AI.BehaviorTrees.Decorators;
using Engine.AI.BehaviorTrees.Nodes;
using Engine.AI.Systems;
using Engine.Core.Time;
using Engine.ECS;

namespace Engine.Tests.AI.BehaviorTrees;

public sealed class BehaviorTreeDecoratorTests
{
    [Theory]
    [InlineData(BehaviorStatus.Success, BehaviorStatus.Failure)]
    [InlineData(BehaviorStatus.Failure, BehaviorStatus.Success)]
    [InlineData(BehaviorStatus.Running, BehaviorStatus.Running)]
    public void Inverter_MapsChildStatus(
        BehaviorStatus childStatus,
        BehaviorStatus expectedStatus)
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var tree = new BehaviorTree(
            "InverterTest",
            new InverterNode(
                new ActionNode(
                    "Child",
                    (in BehaviorTreeContext context) =>
                        childStatus)));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        AssertStatus(
            ai,
            entity,
            tree,
            expectedStatus);
    }

    [Fact]
    public void Repeater_RepeatsUntilSuccess()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int calls = 0;

        var tree = new BehaviorTree(
            "RepeaterTest",
            new RepeaterNode(
                new ActionNode(
                    "RepeatedAction",
                    (in BehaviorTreeContext context) =>
                    {
                        calls++;

                        return calls == 1
                            ? BehaviorStatus.Failure
                            : BehaviorStatus.Success;
                    }),
                RepeatUntil.Success));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(1, calls);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Running);

        AITestHelpers.Update(ai, 0.1, 1);

        Assert.Equal(2, calls);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Success);
    }

    [Fact]
    public void Repeater_DoesNotLoopInSingleUpdate()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int calls = 0;

        var tree = new BehaviorTree(
            "RepeaterLimitTest",
            new RepeaterNode(
                new ActionNode(
                    "AlwaysFail",
                    (in BehaviorTreeContext context) =>
                    {
                        calls++;
                        return BehaviorStatus.Failure;
                    }),
                RepeatUntil.Success));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(1, calls);

        AITestHelpers.Update(ai, 0.1, 1);

        Assert.Equal(2, calls);
    }

    [Fact]
    public void WaitNode_ReturnsRunningUntilDurationExpires()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var tree = new BehaviorTree(
            "WaitTest",
            new WaitNode(
                Duration.FromSeconds(2)));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Running);

        AITestHelpers.Update(ai, 1.5, 1);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Running);

        AITestHelpers.Update(ai, 2.0, 2);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Success);
    }

    [Fact]
    public void WaitNode_RejectsNegativeDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WaitNode(
                Duration.FromSeconds(-1)));
    }

    [Fact]
    public void CooldownNode_PreventsExecutionUntilExpired()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int calls = 0;

        var tree = new BehaviorTree(
            "CooldownTest",
            new CooldownNode(
                new ActionNode(
                    "Attack",
                    (in BehaviorTreeContext context) =>
                    {
                        calls++;
                        return BehaviorStatus.Success;
                    }),
                Duration.FromSeconds(2)));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(1, calls);

        AITestHelpers.Update(ai, 1.0, 1);

        Assert.Equal(1, calls);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Failure);

        AITestHelpers.Update(ai, 2.0, 2);

        Assert.Equal(2, calls);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Success);
    }

    [Fact]
    public void EmptySequence_ReturnsSuccess()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var tree = new BehaviorTree(
            "EmptySequence",
            new SequenceNode());

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Success);
    }

    [Fact]
    public void EmptySelector_ReturnsFailure()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var tree = new BehaviorTree(
            "EmptySelector",
            new SelectorNode());

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Failure);
    }

    private static void AssertStatus(
        AISystem ai,
        Engine.ECS.Entities.EntityId entity,
        BehaviorTree tree,
        BehaviorStatus expected)
    {
        Assert.True(
            ai.TryGetAgentState(
                entity,
                out var state));

        Assert.Equal(
            expected,
            tree.GetLastStatus(state!));
    }
}