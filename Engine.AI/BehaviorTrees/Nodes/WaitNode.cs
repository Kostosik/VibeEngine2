using Engine.AI.BehaviorTrees.Core;
using Engine.Core.Time;

namespace Engine.AI.BehaviorTrees.Nodes;

public sealed class WaitNode : IBehaviorTreeNode
{
    private sealed class RuntimeState
    {
        public TimeSpan StartedAt;

        public RuntimeState(TimeSpan startedAt)
        {
            StartedAt = startedAt;
        }
    }

    private readonly Duration _duration;

    public string Name { get; }

    public WaitNode(
        Duration duration,
        string name = "Wait")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (duration.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration));
        }

        Name = name;
        _duration = duration;
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        if (_duration.IsZero)
        {
            context.RemoveState(this);
            return BehaviorStatus.Success;
        }

        TimeSpan now = context.Time.Elapsed.Value;

        var state = context.GetOrCreateState(
            this,
            () => new RuntimeState(now));

        if (now - state.StartedAt < _duration.Value)
            return BehaviorStatus.Running;

        context.RemoveState(this);

        return BehaviorStatus.Success;
    }

    public void Reset(
        in BehaviorTreeContext context)
    {
        context.RemoveState(this);
    }
}