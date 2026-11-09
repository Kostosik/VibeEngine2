using Engine.AI.BehaviorTrees;
using Engine.AI.BehaviorTrees.Core;
using Engine.AI.BehaviorTrees.Nodes;
using Engine.AI.Components;
using Engine.AI.Systems;
using Engine.ECS;

namespace Engine.Tests.AI.BehaviorTrees;

public sealed class BehaviorTreeTests
{
    [Fact]
    public void Sequence_ExecutesChildrenInOrder()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int firstCalls = 0;
        int secondCalls = 0;

        var tree = new BehaviorTree(
            "SequenceTest",
            new SequenceNode(
                new ActionNode(
                    "First",
                    (in BehaviorTreeContext context) =>
                    {
                        firstCalls++;
                        return BehaviorStatus.Success;
                    }),
                new ActionNode(
                    "Second",
                    (in BehaviorTreeContext context) =>
                    {
                        secondCalls++;

                        return secondCalls == 1
                            ? BehaviorStatus.Running
                            : BehaviorStatus.Success;
                    })));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0, 0);

        Assert.Equal(1, firstCalls);
        Assert.Equal(1, secondCalls);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Running);

        AITestHelpers.Update(ai, 0.1, 1);

        Assert.Equal(1, firstCalls);
        Assert.Equal(2, secondCalls);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Success);
    }

    [Fact]
    public void Selector_PrefersEarlierSuccessfulBranch()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        bool threatDetected = false;

        int fleeCalls = 0;
        int patrolCalls = 0;
        int patrolAborts = 0;

        var tree = new BehaviorTree(
            "PriorityTest",
            new SelectorNode(
                new SequenceNode(
                    new ConditionNode(
                        "ThreatDetected",
                        (in BehaviorTreeContext context) =>
                            threatDetected),
                    new ActionNode(
                        "Flee",
                        (in BehaviorTreeContext context) =>
                        {
                            fleeCalls++;
                            return BehaviorStatus.Success;
                        })),
                new ActionNode(
                    "Patrol",
                    (in BehaviorTreeContext context) =>
                    {
                        patrolCalls++;
                        return BehaviorStatus.Running;
                    },
                    abort: (in BehaviorTreeContext context) =>
                    {
                        patrolAborts++;
                    })));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0, 0);

        Assert.Equal(0, fleeCalls);
        Assert.Equal(1, patrolCalls);

        threatDetected = true;

        AITestHelpers.Update(ai, 0.1, 1);

        Assert.Equal(1, fleeCalls);
        Assert.Equal(1, patrolCalls);
        Assert.Equal(1, patrolAborts);

        AssertStatus(
            ai,
            entity,
            tree,
            BehaviorStatus.Success);
    }

    [Fact]
    public void ActionNode_RunningActionCanBeAborted()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int abortCalls = 0;

        var tree = new BehaviorTree(
            "AbortTest",
            new ActionNode(
                "LongRunningAction",
                (in BehaviorTreeContext context) =>
                    BehaviorStatus.Running,
                abort: (in BehaviorTreeContext context) =>
                {
                    abortCalls++;
                }));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        Assert.True(ai.RemoveBehavior(entity));

        Assert.Equal(1, abortCalls);
    }

    [Fact]
    public void BehaviorTree_RestartsAfterCompletion()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int calls = 0;

        var tree = new BehaviorTree(
            "RestartTest",
            new ActionNode(
                "Action",
                (in BehaviorTreeContext context) =>
                {
                    calls++;
                    return BehaviorStatus.Success;
                }));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0, 0);
        AITestHelpers.Update(ai, 0.1, 1);

        Assert.Equal(2, calls);
    }

    [Fact]
    public void BehaviorTree_CanStopAfterCompletion()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int calls = 0;

        var tree = new BehaviorTree(
            "OneShotTest",
            new ActionNode(
                "Action",
                (in BehaviorTreeContext context) =>
                {
                    calls++;
                    return BehaviorStatus.Success;
                }),
            restartOnCompletion: false);

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0, 0);
        AITestHelpers.Update(ai, 0.1, 1);
        AITestHelpers.Update(ai, 0.2, 2);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void BehaviorTree_RejectsReusedNode()
    {
        var sharedNode = new ActionNode(
            "Shared",
            (in BehaviorTreeContext context) =>
                BehaviorStatus.Success);

        var root = new SequenceNode(
            sharedNode,
            sharedNode);

        Assert.Throws<InvalidOperationException>(
            () => new BehaviorTree(
                "InvalidTree",
                root));
    }

    [Fact]
    public void BehaviorTree_RejectsCyclicTree()
    {
        var root = new CyclicNode();

        Assert.Throws<InvalidOperationException>(
            () => new BehaviorTree(
                "CyclicTree",
                root));
    }

    [Fact]
    public void BehaviorTree_NodeStateIsIndependentBetweenAgents()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var first = AITestHelpers.CreateAgent(world);
        var second = AITestHelpers.CreateAgent(world);

        ref var secondComponent =
            ref world.Get<AIComponent>(second);

        secondComponent.Enabled = false;

        var tree = new BehaviorTree(
            "IndependentWait",
            new Engine.AI.BehaviorTrees.Nodes.WaitNode(
                Engine.Core.Time.Duration.FromSeconds(1)));

        ai.SetBehavior(first, tree);
        ai.SetBehavior(second, tree);

        AITestHelpers.Update(ai, 0.0, 0);

        ref var firstComponent =
            ref world.Get<AIComponent>(first);

        firstComponent.Enabled = false;
        secondComponent.Enabled = true;

        AITestHelpers.Update(ai, 1.5, 1);

        AITestHelpers.Update(ai, 2.1, 2);

        AssertStatus(
            ai,
            second,
            tree,
            BehaviorStatus.Running);
    }

    [Fact]
    public void EntityDestroyed_AbortsRunningBehavior()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int abortCalls = 0;

        var tree = new BehaviorTree(
            "DestroyTest",
            new ActionNode(
                "Running",
                (in BehaviorTreeContext context) =>
                    BehaviorStatus.Running,
                abort: (in BehaviorTreeContext context) =>
                {
                    abortCalls++;
                }));

        ai.SetBehavior(entity, tree);

        AITestHelpers.Update(ai, 0.0);

        Assert.True(world.DestroyEntity(entity));

        Assert.Equal(1, abortCalls);
        Assert.Equal(0, ai.AgentCount);
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

    private sealed class CyclicNode : IBehaviorTreeParentNode
    {
        public string Name => "Cycle";

        public IReadOnlyList<IBehaviorTreeNode> Children =>
            new IBehaviorTreeNode[] { this };

        public BehaviorStatus Tick(
            in BehaviorTreeContext context)
        {
            return BehaviorStatus.Running;
        }

        public void Reset(
            in BehaviorTreeContext context)
        {
        }
    }
}