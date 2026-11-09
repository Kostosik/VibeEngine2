using Engine.AI.Behaviors;
using Engine.AI.Components;
using Engine.AI.Core;
using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.AI.Systems;

public sealed class AISystem :
    IUpdateSystem,
    IDisposable
{
    private readonly World _world;

    private readonly Dictionary<EntityId, IAIBehavior>
        _behaviors = new();

    private readonly Dictionary<EntityId, AIAgentState>
        _agentStates = new();

    private readonly List<EntityId>
        _agentsToUpdate = new();

    private readonly List<EntityId>
        _staleAgents = new();

    private bool _disposed;
    private bool _isUpdating;

    public int AgentCount => _behaviors.Count;

    public AISystem(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _world.EntityDestroyed += OnEntityDestroyed;
    }

    public void SetBehavior(
        EntityId entity,
        IAIBehavior behavior)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(behavior);

        if (!_world.Exists(entity))
        {
            throw new ArgumentException(
                "Entity does not exist.",
                nameof(entity));
        }

        if (!_world.Has<AIComponent>(entity))
        {
            throw new InvalidOperationException(
                "Entity does not have an AIComponent.");
        }

        if (_behaviors.TryGetValue(entity, out var previous))
        {
            _agentStates[entity].Remove(previous);
        }

        _behaviors[entity] = behavior;

        if (!_agentStates.ContainsKey(entity))
        {
            _agentStates.Add(
                entity,
                new AIAgentState());
        }
    }

    public bool RemoveBehavior(EntityId entity)
    {
        EnsureNotDisposed();

        if (!_behaviors.Remove(entity, out var behavior))
            return false;

        if (_agentStates.TryGetValue(entity, out var state))
        {
            state.Remove(behavior);
        }

        return true;
    }

    public bool TryGetBehavior(
        EntityId entity,
        out IAIBehavior? behavior)
    {
        EnsureNotDisposed();

        return _behaviors.TryGetValue(
            entity,
            out behavior);
    }

    public void Update(SystemContext context)
    {
        EnsureNotDisposed();

        if (_isUpdating)
        {
            throw new InvalidOperationException(
                "AI system cannot be updated recursively.");
        }

        _isUpdating = true;

        try
        {
            RemoveInvalidAgents();
            CollectAgents();
            UpdateAgents(context);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void RemoveInvalidAgents()
    {
        _staleAgents.Clear();

        foreach (var pair in _behaviors)
        {
            if (!_world.Exists(pair.Key) ||
                !_world.Has<AIComponent>(pair.Key))
            {
                _staleAgents.Add(pair.Key);
            }
        }

        foreach (var entity in _staleAgents)
        {
            if (_behaviors.Remove(entity, out var behavior) &&
                _agentStates.TryGetValue(entity, out var state))
            {
                state.Remove(behavior);
            }

            _agentStates.Remove(entity);
        }
    }

    private void CollectAgents()
    {
        _agentsToUpdate.Clear();

        foreach (var item in _world.Query<AIComponent>())
        {
            if (!item.Component.Enabled)
                continue;

            if (_behaviors.ContainsKey(item.Entity))
            {
                _agentsToUpdate.Add(item.Entity);
            }
        }
    }

    private void UpdateAgents(SystemContext context)
    {
        for (var i = 0; i < _agentsToUpdate.Count; i++)
        {
            var entity = _agentsToUpdate[i];

            if (!_world.Exists(entity))
                continue;

            if (!_behaviors.TryGetValue(entity, out var behavior))
                continue;

            if (!_world.Has<AIComponent>(entity))
                continue;

            ref readonly var ai =
                ref _world.GetReadOnly<AIComponent>(entity);

            if (!ai.Enabled)
                continue;

            if (!_agentStates.TryGetValue(entity, out var state))
            {
                state = new AIAgentState();
                _agentStates.Add(entity, state);
            }

            var aiContext = new AIContext(
                _world,
                entity,
                context.Time,
                state);

            behavior.Update(in aiContext);
        }
    }

    private void OnEntityDestroyed(EntityId entity)
    {
        _behaviors.Remove(entity);

        if (_agentStates.Remove(entity, out var state))
        {
            state.Clear();
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public bool TryGetAgentState(
    EntityId entity,
    out AIAgentState? state)
    {
        EnsureNotDisposed();

        return _agentStates.TryGetValue(
            entity,
            out state);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_isUpdating)
        {
            throw new InvalidOperationException(
                "Cannot dispose AI system while it is updating.");
        }

        _world.EntityDestroyed -= OnEntityDestroyed;

        foreach (var state in _agentStates.Values)
        {
            state.Clear();
        }

        _behaviors.Clear();
        _agentStates.Clear();

        _agentsToUpdate.Clear();
        _staleAgents.Clear();

        _disposed = true;
    }
}