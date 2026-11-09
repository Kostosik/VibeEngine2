using Engine.AI.Behaviors;
using Engine.AI.Components;
using Engine.AI.Core;
using Engine.AI.Memory;
using Engine.AI.Perception;
using Engine.AI.Scheduling;
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

    private readonly Dictionary<EntityId, AIMemory>
        _memories = new();

    private readonly List<EntityId>
        _eligibleAgents = new();

    private readonly List<EntityId>
        _staleAgents = new();

    private bool _disposed;
    private bool _isUpdating;

    public AIScheduler Scheduler { get; }
    public PerceptionSystem Perception { get; }
    public int AgentCount => _behaviors.Count;

    public AISystem(
        World world,
        AISchedulerOptions? schedulerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;

        Scheduler = new AIScheduler(schedulerOptions);
        Perception = new PerceptionSystem(world);
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
            if (_agentStates.TryGetValue(
                    entity,
                    out var previousState))
            {
                DetachBehavior(previous, previousState);
            }
        }

        _behaviors[entity] = behavior;

        if (!_agentStates.ContainsKey(entity))
        {
            _agentStates.Add(
                entity,
                new AIAgentState());
        }
        if (!_memories.ContainsKey(entity))
        {
            _memories.Add(entity, new AIMemory());
        }
        Scheduler.RegisterOrUpdate(
            entity,
            GetUpdateSettings(entity),
            TimeSpan.Zero);
    }

    public bool RemoveBehavior(EntityId entity)
    {
        EnsureNotDisposed();

        if (!_behaviors.Remove(
                entity,
                out var behavior))
        {
            return false;
        }

        Scheduler.Unregister(entity);

        try
        {
            if (_agentStates.TryGetValue(
                    entity,
                    out var state))
            {
                DetachBehavior(behavior, state);
            }
        }
        finally
        {
            if (_memories.Remove(
                    entity,
                    out var memory))
            {
                memory.Clear();
            }
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

    public bool TryGetAgentState(
        EntityId entity,
        out AIAgentState? state)
    {
        EnsureNotDisposed();

        return _agentStates.TryGetValue(
            entity,
            out state);
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
            Perception.Update(context);
            TimeSpan now = context.Time.Elapsed.Value;

            RemoveInvalidAgents();
            UpdateLastKnownTargetMemories(now);
            CollectEligibleAgents(now);

            UpdateScheduledAgents(context, now);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateLastKnownTargetMemories(TimeSpan now)
    {
        foreach (var pair in _memories)
        {
            var lastKnownTargets = pair.Value.GetOrCreate(
                AIMemoryKeys.LastKnownTargets,
                static () => new AILastKnownTargetMemory());

            if (Perception.TryGetState(
                    pair.Key,
                    out var perceptionState) &&
                perceptionState is not null)
            {
                lastKnownTargets.Observe(
                    perceptionState,
                    now);
            }
            else
            {
                lastKnownTargets.PruneExpired(now);
            }
        }
    }

    private void CollectEligibleAgents(TimeSpan now)
    {
        _eligibleAgents.Clear();

        foreach (var pair in _behaviors)
        {
            EntityId entity = pair.Key;

            if (!_world.Exists(entity))
                continue;

            if (!_world.Has<AIComponent>(entity))
                continue;

            ref readonly var ai =
                ref _world.GetReadOnly<AIComponent>(entity);

            if (!ai.Enabled)
                continue;
            if (_memories.TryGetValue(entity, out var memory))
            {
                memory.PruneExpired(now);
            }
            Scheduler.RegisterOrUpdate(
                entity,
                GetUpdateSettings(entity),
                now);

            _eligibleAgents.Add(entity);
        }
    }

    private AIUpdateSettings GetUpdateSettings(
        EntityId entity)
    {
        if (_world.Has<AIUpdateSettings>(entity))
        {
            return _world.GetReadOnly<AIUpdateSettings>(
                entity);
        }

        return new AIUpdateSettings();
    }

    private void UpdateScheduledAgents(
        SystemContext context,
        TimeSpan now)
    {
        var dueAgents = Scheduler.CollectDueAgents(
            now,
            _eligibleAgents);

        for (int i = 0; i < dueAgents.Count; i++)
        {
            EntityId entity = dueAgents[i];

            if (!_world.Exists(entity))
                continue;

            if (!_behaviors.TryGetValue(
                    entity,
                    out var behavior))
            {
                continue;
            }

            if (!_world.Has<AIComponent>(entity))
                continue;

            ref readonly var ai =
                ref _world.GetReadOnly<AIComponent>(entity);

            if (!ai.Enabled)
                continue;

            if (!_agentStates.TryGetValue(
                    entity,
                    out var state))
            {
                state = new AIAgentState();

                _agentStates.Add(entity, state);
            }

            Perception.TryGetState(
                entity,
                out var perceptionState);

            if (!_memories.TryGetValue(
                entity,
                out var memory))
            {
                memory = new AIMemory();
                _memories.Add(entity, memory);
            }

            var aiContext = new AIContext(
                _world,
                entity,
                context.Time,
                state,
                perceptionState,
                memory);

            behavior.Update(in aiContext);

            // Mark only after successful behavior execution.
            Scheduler.MarkUpdated(entity, now);
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
            bool hasBehavior = _behaviors.Remove(
                entity,
                out var behavior);

            Scheduler.Unregister(entity);

            if (_memories.Remove(entity, out var memory))
            {
                memory.Clear();
            }

            if (!_agentStates.Remove(
                    entity,
                    out var state))
            {
                continue;
            }

            try
            {
                if (hasBehavior)
                {
                    DetachBehavior(
                        behavior!,
                        state);
                }
            }
            finally
            {
                state.Clear();
            }
        }
    }

    private void OnEntityDestroyed(EntityId entity)
    {
        bool hasBehavior = _behaviors.Remove(
            entity,
            out var behavior);

        Scheduler.Unregister(entity);
        if (_memories.Remove(entity, out var memory))
        {
            memory.Clear();
        }
        if (!_agentStates.Remove(
                entity,
                out var state))
        {
            return;
        }

        try
        {
            if (hasBehavior)
            {
                DetachBehavior(
                    behavior!,
                    state);
            }
        }
        finally
        {
            state.Clear();
        }
    }

    private static void DetachBehavior(
        IAIBehavior behavior,
        AIAgentState state)
    {
        try
        {
            if (behavior is IAIBehaviorLifecycle lifecycle)
            {
                lifecycle.OnDetached(state);
            }
        }
        finally
        {
            state.Remove(behavior);
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
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

        var behaviors = _behaviors.ToArray();

        try
        {
            foreach (var pair in behaviors)
            {
                if (_agentStates.TryGetValue(
                        pair.Key,
                        out var state))
                {
                    DetachBehavior(
                        pair.Value,
                        state);
                }
            }
        }
        finally
        {
            try
            {
                Perception.Dispose();
            }
            finally
            {
                foreach (var state in _agentStates.Values)
                {
                    state.Clear();
                }

                _behaviors.Clear();
                foreach (var memory in _memories.Values)
                {
                    memory.Clear();
                }

                _memories.Clear();
                _agentStates.Clear();

                _eligibleAgents.Clear();
                _staleAgents.Clear();

                Scheduler.Clear();

                _disposed = true;
            }
        }
    }
}