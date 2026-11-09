using Engine.AI.BehaviorTrees.Core;

namespace Engine.AI.BehaviorTrees.Nodes;

public sealed class SequenceNode : IBehaviorTreeParentNode
{
    private sealed class RuntimeState
    {
        public int CurrentIndex;
    }

    private readonly IBehaviorTreeNode[] _children;

    public string Name { get; }

    public IReadOnlyList<IBehaviorTreeNode> Children { get; }

    public SequenceNode(
        params IBehaviorTreeNode[] children)
        : this("Sequence", children)
    {
    }

    public SequenceNode(
        string name,
        params IBehaviorTreeNode[] children)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(children);

        ValidateChildren(children);

        Name = name;
        _children = children.ToArray();
        Children = Array.AsReadOnly(_children);
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        if (_children.Length == 0)
            return BehaviorStatus.Success;

        var state = context.GetOrCreateState(
            this,
            static () => new RuntimeState());

        while (state.CurrentIndex < _children.Length)
        {
            var child = _children[state.CurrentIndex];

            var result = child.Tick(in context);

            if (result == BehaviorStatus.Running)
                return BehaviorStatus.Running;

            child.Reset(in context);

            if (result == BehaviorStatus.Failure)
            {
                Reset(in context);
                return BehaviorStatus.Failure;
            }

            state.CurrentIndex++;
        }

        Reset(in context);

        return BehaviorStatus.Success;
    }

    public void Reset(
        in BehaviorTreeContext context)
    {
        foreach (var child in _children)
        {
            child.Reset(in context);
        }

        context.RemoveState(this);
    }

    private static void ValidateChildren(
        IBehaviorTreeNode[] children)
    {
        foreach (var child in children)
        {
            ArgumentNullException.ThrowIfNull(child);
        }
    }
}