using Engine.AI.BehaviorTrees.Core;

namespace Engine.AI.BehaviorTrees.Decorators;

public enum RepeatUntil
{
    Success,
    Failure
}

public sealed class RepeaterNode : IBehaviorTreeParentNode
{
    private readonly IBehaviorTreeNode _child;

    private readonly IReadOnlyList<IBehaviorTreeNode> _children;

    public string Name { get; }

    public RepeatUntil Target { get; }

    public IReadOnlyList<IBehaviorTreeNode> Children => _children;

    public RepeaterNode(
        IBehaviorTreeNode child,
        RepeatUntil target = RepeatUntil.Success,
        string name = "Repeater")
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Enum.IsDefined(target))
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        _child = child;
        _children = Array.AsReadOnly(new[] { child });

        Target = target;
        Name = name;
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        var result = _child.Tick(in context);

        if (result == BehaviorStatus.Running)
            return BehaviorStatus.Running;

        _child.Reset(in context);

        bool targetReached = Target switch
        {
            RepeatUntil.Success =>
                result == BehaviorStatus.Success,

            RepeatUntil.Failure =>
                result == BehaviorStatus.Failure,

            _ => throw new ArgumentOutOfRangeException(
                nameof(Target))
        };

        if (targetReached)
            return BehaviorStatus.Success;

        return BehaviorStatus.Running;
    }

    public void Reset(
        in BehaviorTreeContext context)
    {
        _child.Reset(in context);
    }
}