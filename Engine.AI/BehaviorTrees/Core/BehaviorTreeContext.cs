using Engine.AI.Core;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.AI.BehaviorTrees.Core;

internal sealed class BehaviorTreeRuntimeState
{
    private readonly Dictionary<object, object> _nodeStates =
        new(ReferenceEqualityComparer.Instance);

    internal T GetOrCreate<T>(
        object key,
        Func<T> factory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (_nodeStates.TryGetValue(key, out var existing))
        {
            if (existing is T typed)
                return typed;

            throw new InvalidOperationException(
                "Node state is already registered with another type.");
        }

        var state = factory();

        ArgumentNullException.ThrowIfNull(state);

        _nodeStates.Add(key, state);

        return state;
    }

    internal bool TryGet<T>(
        object key,
        out T? state)
        where T : class
    {
        if (_nodeStates.TryGetValue(key, out var existing) &&
            existing is T typed)
        {
            state = typed;
            return true;
        }

        state = null;
        return false;
    }

    internal bool Remove(object key)
    {
        return _nodeStates.Remove(key);
    }

    internal void Clear()
    {
        _nodeStates.Clear();
    }
}

public readonly struct BehaviorTreeContext
{
    private readonly BehaviorTreeRuntimeState _runtimeState;

    public AIContext AI { get; }

    public World World => AI.World;

    public EntityId Entity => AI.Entity;

    public TimeSnapshot Time => AI.Time;

    internal BehaviorTreeContext(
        AIContext ai,
        BehaviorTreeRuntimeState runtimeState)
    {
        AI = ai;
        _runtimeState = runtimeState;
    }

    public T GetOrCreateState<T>(
        object key,
        Func<T> factory)
        where T : class
    {
        return _runtimeState.GetOrCreate(
            key,
            factory);
    }

    public bool TryGetState<T>(
        object key,
        out T? state)
        where T : class
    {
        return _runtimeState.TryGet(
            key,
            out state);
    }

    public bool RemoveState(object key)
    {
        return _runtimeState.Remove(key);
    }
}