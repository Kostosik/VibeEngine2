using Engine.AI.BehaviorTrees.Core;
using Engine.Core.Time;

namespace Engine.AI.BehaviorTrees.Decorators;

public sealed class CooldownNode : IBehaviorTreeParentNode
{
    private sealed class RuntimeState
    {
        public TimeSpan CooldownUntil;

        public RuntimeState(TimeSpan cooldownUntil)
        {
            CooldownUntil = cooldownUntil;
        }
    }

    private readonly IBehaviorTreeNode _child;

    private readonly IReadOnlyList<IBehaviorTreeNode> _children;

    private Duration _duration;

    public string Name { get; }

    public Duration Duration
    {
        get => _duration;

        set
        {
            if (value.IsNegative)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _duration = value;
        }
    }

    public IReadOnlyList<IBehaviorTreeNode> Children => _children;

    public CooldownNode(
        IBehaviorTreeNode child,
        Duration duration,
        string name = "Cooldown")
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (duration.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration));
        }

        _child = child;
        _children = Array.AsReadOnly(new[] { child });

        _duration = duration;
        Name = name;
    }

    public BehaviorStatus Tick(
        in BehaviorTreeContext context)
    {
        TimeSpan now = context.Time.Elapsed.Value;

        if (context.TryGetState(
                this,
                out RuntimeState? state))
        {
            if (now < state.CooldownUntil)
                return BehaviorStatus.Failure;

            context.RemoveState(this);
        }

        var result = _child.Tick(in context);

        if (result == BehaviorStatus.Success &&
            !_duration.IsZero)
        {
            _child.Reset(in context);

            context.GetOrCreateState(
                this,
                () => new RuntimeState(
                    now + _duration.Value));
        }

        return result;
    }

    public void Reset(
        in BehaviorTreeContext context)
    {
        // The cooldown intentionally survives branch resets.
        _child.Reset(in context);
    }
}