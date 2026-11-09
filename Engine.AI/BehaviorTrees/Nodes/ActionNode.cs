using Engine.AI.BehaviorTrees.Core;

namespace Engine.AI.BehaviorTrees.Nodes;

public delegate BehaviorStatus BehaviorTreeAction(
    in BehaviorTreeContext context);

public delegate void BehaviorTreeAbortHandler(
    in BehaviorTreeContext context);

public sealed class ActionNode : IBehaviorTreeNode
{
    private sealed class RuntimeState
    {
        public bool IsRunning;
    }

    private readonly BehaviorTreeAction _action;

    private readonly BehaviorTreeAbortHandler? _abort;

    public string Name { get; }

    public ActionNode(
        string name,
        BehaviorTreeAction action,
        BehaviorTreeAbortHandler? abort = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(action);

        Name = name;
        _action = action;
        _abort = abort;
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        var result = _action(in context);

        if (result is not (
            BehaviorStatus.Running or
            BehaviorStatus.Success or
            BehaviorStatus.Failure))
        {
            throw new InvalidOperationException(
                $"Action '{Name}' returned an invalid status.");
        }

        if (result == BehaviorStatus.Running)
        {
            var state = context.GetOrCreateState(
                this,
                static () => new RuntimeState());

            state.IsRunning = true;
        }
        else
        {
            context.RemoveState(this);
        }

        return result;
    }

    public void Reset(
        in BehaviorTreeContext context)
    {
        try
        {
            if (_abort != null &&
                context.TryGetState(
                    this,
                    out RuntimeState? state) &&
                state.IsRunning)
            {
                _abort(in context);
            }
        }
        finally
        {
            context.RemoveState(this);
        }
    }
}