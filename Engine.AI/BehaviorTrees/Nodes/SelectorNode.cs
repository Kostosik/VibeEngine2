using Engine.AI.BehaviorTrees.Core;

namespace Engine.AI.BehaviorTrees.Nodes;

public sealed class SelectorNode : IBehaviorTreeParentNode
{
    private sealed class RuntimeState
    {
        public int ActiveChildIndex = -1;
    }

    private readonly IBehaviorTreeNode[] _children;

    public string Name { get; }

    public IReadOnlyList<IBehaviorTreeNode> Children { get; }

    public SelectorNode(
        params IBehaviorTreeNode[] children)
        : this("Selector", children)
    {
    }

    public SelectorNode(
        string name,
        params IBehaviorTreeNode[] children)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(children);

        foreach (var child in children)
        {
            ArgumentNullException.ThrowIfNull(child);
        }

        Name = name;
        _children = children.ToArray();
        Children = Array.AsReadOnly(_children);
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        if (_children.Length == 0)
            return BehaviorStatus.Failure;

        var state = context.GetOrCreateState(
            this,
            static () => new RuntimeState());

        int previousActiveIndex =
            state.ActiveChildIndex;

        // Always start from the highest-priority child.
        for (int i = 0; i < _children.Length; i++)
        {
            var child = _children[i];

            var result = child.Tick(in context);

            if (result == BehaviorStatus.Failure)
            {
                child.Reset(in context);
                continue;
            }

            // A higher-priority branch interrupted
            // a previously running lower-priority branch.
            if (previousActiveIndex >= 0 &&
                previousActiveIndex != i)
            {
                _children[previousActiveIndex]
                    .Reset(in context);
            }

            if (result == BehaviorStatus.Running)
            {
                state.ActiveChildIndex = i;
                return BehaviorStatus.Running;
            }

            Reset(in context);

            return BehaviorStatus.Success;
        }

        Reset(in context);

        return BehaviorStatus.Failure;
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
}