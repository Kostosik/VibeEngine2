using Engine.AI.Components;
using Engine.AI.Perception.Core;
using Engine.AI.Perception.Sensors;
using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.AI.Perception;

public sealed class PerceptionSystem :
    IUpdateSystem,
    IDisposable
{
    private sealed class AgentSensors
    {
        public readonly List<IPerceptionSensor> Sensors = new();

        public readonly PerceptionState State = new();

        public readonly PerceptionBuffer Buffer;

        public AgentSensors()
        {
            Buffer = new PerceptionBuffer(State);
        }
    }

    private readonly World _world;

    private readonly Dictionary<EntityId, AgentSensors>
        _agents = new();

    private readonly List<EntityId>
        _orderedAgents = new();

    private readonly List<EntityId>
        _staleAgents = new();

    private bool _isUpdating;
    private bool _disposed;

    public int AgentCount => _agents.Count;

    public int SensorCount
    {
        get
        {
            int count = 0;

            foreach (var agent in _agents.Values)
            {
                count += agent.Sensors.Count;
            }

            return count;
        }
    }

    public PerceptionSystem(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;

        _world.EntityDestroyed += OnEntityDestroyed;
    }

    public void RegisterSensor(
        EntityId observer,
        IPerceptionSensor sensor)
    {
        EnsureNotDisposed();
        EnsureNotUpdating();

        ArgumentNullException.ThrowIfNull(sensor);

        if (!_world.Exists(observer))
        {
            throw new ArgumentException(
                "Observer entity does not exist.",
                nameof(observer));
        }

        if (!_world.Has<AIComponent>(observer))
        {
            throw new InvalidOperationException(
                "Observer entity does not have an AIComponent.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            sensor.Name);

        if (!_agents.TryGetValue(
                observer,
                out var agent))
        {
            agent = new AgentSensors();

            _agents.Add(observer, agent);
        }

        foreach (var existing in agent.Sensors)
        {
            if (ReferenceEquals(existing, sensor))
            {
                throw new InvalidOperationException(
                    "Sensor is already registered.");
            }

            if (string.Equals(
                    existing.Name,
                    sensor.Name,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Sensor name '{sensor.Name}' is already registered.");
            }
        }

        agent.Sensors.Add(sensor);
    }

    public bool RemoveSensor(
        EntityId observer,
        IPerceptionSensor sensor)
    {
        EnsureNotDisposed();
        EnsureNotUpdating();

        ArgumentNullException.ThrowIfNull(sensor);

        if (!_agents.TryGetValue(
                observer,
                out var agent))
        {
            return false;
        }

        bool removed = agent.Sensors.Remove(sensor);

        if (removed && agent.Sensors.Count == 0)
        {
            _agents.Remove(observer);
            agent.State.Clear();
        }

        return removed;
    }

    public bool RemoveAgent(EntityId observer)
    {
        EnsureNotDisposed();
        EnsureNotUpdating();

        if (!_agents.Remove(
                observer,
                out var agent))
        {
            return false;
        }

        agent.State.Clear();

        return true;
    }

    public bool TryGetState(
        EntityId observer,
        out PerceptionState? state)
    {
        EnsureNotDisposed();

        if (_agents.TryGetValue(
                observer,
                out var agent))
        {
            state = agent.State;
            return true;
        }

        state = null;
        return false;
    }

    public void Update(SystemContext context)
    {
        EnsureNotDisposed();

        if (_isUpdating)
        {
            throw new InvalidOperationException(
                "Perception system cannot be updated recursively.");
        }

        _isUpdating = true;

        try
        {
            CollectAgents();

            TimeSpan now = context.Time.Elapsed.Value;

            for (int i = 0; i < _orderedAgents.Count; i++)
            {
                EntityId observer = _orderedAgents[i];

                if (!_agents.TryGetValue(
                        observer,
                        out var agent))
                {
                    continue;
                }

                if (!_world.Exists(observer) ||
                    !_world.Has<AIComponent>(observer))
                {
                    RemoveAgentDuringUpdate(observer);
                    continue;
                }

                ref readonly var ai =
                    ref _world.GetReadOnly<AIComponent>(
                        observer);

                if (!ai.Enabled)
                    continue;

                UpdateAgent(
                    observer,
                    agent,
                    context,
                    now);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void CollectAgents()
    {
        _orderedAgents.Clear();
        _staleAgents.Clear();

        foreach (var pair in _agents)
        {
            if (!_world.Exists(pair.Key) ||
                !_world.Has<AIComponent>(pair.Key))
            {
                _staleAgents.Add(pair.Key);
                continue;
            }

            _orderedAgents.Add(pair.Key);
        }

        foreach (var entity in _staleAgents)
        {
            RemoveAgentDuringUpdate(entity);
        }

        _orderedAgents.Sort(
            static (left, right) =>
            {
                int comparison =
                    left.Index.CompareTo(right.Index);

                if (comparison != 0)
                    return comparison;

                return left.Generation.CompareTo(
                    right.Generation);
            });
    }

    private void UpdateAgent(
        EntityId observer,
        AgentSensors agent,
        SystemContext context,
        TimeSpan now)
    {
        agent.State.BeginUpdate();

        var perceptionContext = new PerceptionContext(
            _world,
            observer,
            context.Time);

        try
        {
            foreach (var sensor in agent.Sensors)
            {
                if (!_world.Exists(observer) ||
                    !_agents.TryGetValue(
                        observer,
                        out var currentAgent) ||
                    !ReferenceEquals(currentAgent, agent))
                {
                    agent.State.AbortUpdate();
                    return;
                }

                agent.Buffer.BeginSensor(
                    sensor.Name,
                    now);

                sensor.Sense(
                    in perceptionContext,
                    agent.Buffer);
            }

            if (!_world.Exists(observer) ||
                !_agents.TryGetValue(
                    observer,
                    out var registeredAgent) ||
                !ReferenceEquals(registeredAgent, agent))
            {
                agent.State.AbortUpdate();
                return;
            }

            agent.State.CommitUpdate(now);
        }
        catch
        {
            agent.State.AbortUpdate();
            throw;
        }
    }

    private void RemoveAgentDuringUpdate(EntityId observer)
    {
        if (_agents.Remove(
                observer,
                out var agent))
        {
            agent.State.Clear();
        }
    }

    private void OnEntityDestroyed(EntityId entity)
    {
        if (_agents.Remove(
                entity,
                out var agent))
        {
            agent.State.Clear();
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private void EnsureNotUpdating()
    {
        if (_isUpdating)
        {
            throw new InvalidOperationException(
                "Sensor registration cannot change during perception update.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_isUpdating)
        {
            throw new InvalidOperationException(
                "Cannot dispose perception system while it is updating.");
        }

        _world.EntityDestroyed -= OnEntityDestroyed;

        foreach (var agent in _agents.Values)
        {
            agent.State.Clear();
        }

        _agents.Clear();
        _orderedAgents.Clear();
        _staleAgents.Clear();

        _disposed = true;
    }
}