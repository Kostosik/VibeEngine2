using Engine.AI.BehaviorTrees.Core;

namespace Engine.AI.BehaviorTrees.Nodes;

public delegate bool BehaviorTreeCondition(
    in BehaviorTreeContext context);

public sealed class ConditionNode : IBehaviorTreeNode
{
    private readonly BehaviorTreeCondition _condition;

    public string Name { get; }

    public ConditionNode(
        string name,
        BehaviorTreeCondition condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(condition);

        Name = name;
        _condition = condition;
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        return _condition(in context)
            ? BehaviorStatus.Success
            : BehaviorStatus.Failure;
    }

    public void Reset(
        in BehaviorTreeContext context)
    {
    }
}