using Engine.AI.BehaviorTrees.Core;

namespace Engine.AI.BehaviorTrees.Decorators;

public sealed class InverterNode : IBehaviorTreeParentNode
{
    private readonly IBehaviorTreeNode _child;

    private readonly IReadOnlyList<IBehaviorTreeNode> _children;

    public string Name { get; }

    public IReadOnlyList<IBehaviorTreeNode> Children => _children;

    public InverterNode(
        IBehaviorTreeNode child,
        string name = "Inverter")
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _child = child;
        _children = Array.AsReadOnly(new[] { child });
        Name = name;
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        return _child.Tick(in context) switch
        {
            BehaviorStatus.Success => BehaviorStatus.Failure,
            BehaviorStatus.Failure => BehaviorStatus.Success,
            BehaviorStatus.Running => BehaviorStatus.Running,
            _ => throw new InvalidOperationException(
                "Child returned an invalid behavior status.")
        };
    }

    public void Reset(
        in BehaviorTreeContext context)
    {
        _child.Reset(in context);
    }
}