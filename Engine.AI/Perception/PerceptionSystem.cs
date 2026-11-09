
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
    private sealed class SensorRegistration
    {
        public IPerceptionSensor Sensor { get; }

        public string Name { get; }

        public PerceptionSensorSettings Settings { get; }

        public TimeSpan NextUpdateAt { get; set; }

        public long LastServedOrder { get; set; }

        public SensorRegistration(
            IPerceptionSensor sensor,
            string name,
            PerceptionSensorSettings settings)
        {
            Sensor = sensor;
            Name = name;
            Settings = settings;

            NextUpdateAt = TimeSpan.Zero;
        }
    }

    private sealed class AgentSensors
    {
        public readonly List<SensorRegistration> Sensors = new();

        public readonly List<SensorRegistration> SelectedSensors = new();

        public readonly HashSet<string> ActiveSensorNames =
            new(StringComparer.Ordinal);

        public readonly HashSet<string> UpdatedSensorNames =
            new(StringComparer.Ordinal);

        public readonly PerceptionState State = new();

        public readonly PerceptionBuffer Buffer;

        public AgentSensors()
        {
            Buffer = new PerceptionBuffer(State);
        }
    }

    private readonly record struct DueSensor(
        EntityId Observer,
        AgentSensors Agent,
        SensorRegistration Registration,
        long EffectivePriority,
        long Sequence);

    private readonly World _world;

    private readonly Dictionary<EntityId, AgentSensors>
        _agents = new();

    private readonly List<EntityId>
        _orderedAgents = new();

    private readonly List<EntityId>
        _staleAgents = new();

    private readonly List<DueSensor>
        _dueSensors = new();

    private long _serviceOrder;

    private bool _isUpdating;
    private bool _disposed;

    public PerceptionSystemOptions Options { get; }

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

    public PerceptionSystem(
        World world,
        PerceptionSystemOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        Options = options ?? new PerceptionSystemOptions();

        _world.EntityDestroyed += OnEntityDestroyed;
    }

    public void RegisterSensor(
        EntityId observer,
        IPerceptionSensor sensor)
    {
        RegisterSensor(
            observer,
            sensor,
            new PerceptionSensorSettings());
    }

    public void RegisterSensor(
        EntityId observer,
        IPerceptionSensor sensor,
        PerceptionSensorSettings settings)
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

        string sensorName = sensor.Name;

        ArgumentException.ThrowIfNullOrWhiteSpace(
            sensorName);

        if (!_agents.TryGetValue(
                observer,
                out var agent))
        {
            agent = new AgentSensors();

            _agents.Add(observer, agent);
        }

        foreach (var existing in agent.Sensors)
        {
            if (ReferenceEquals(existing.Sensor, sensor))
            {
                throw new InvalidOperationException(
                    "Sensor is already registered.");
            }

            if (string.Equals(
                    existing.Name,
                    sensorName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Sensor name '{sensorName}' is already registered.");
            }
        }

        agent.Sensors.Add(
            new SensorRegistration(
                sensor,
                sensorName,
                settings));

        agent.ActiveSensorNames.Add(sensorName);
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

        int index = agent.Sensors.FindIndex(
            registration =>
                ReferenceEquals(
                    registration.Sensor,
                    sensor));

        if (index < 0)
            return false;

        string sensorName = agent.Sensors[index].Name;

        agent.Sensors.RemoveAt(index);
        agent.ActiveSensorNames.Remove(sensorName);
        agent.State.RemoveSensor(sensorName);

        if (agent.Sensors.Count == 0)
        {
            _agents.Remove(observer);
            agent.State.Clear();
        }

        return true;
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

            CollectDueSensors(now);

            SelectSensorsWithinBudget();

            UpdateSelectedAgents(context, now);
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

    private void CollectDueSensors(TimeSpan now)
    {
        _dueSensors.Clear();

        long agingTicks =
            Options.PriorityAgingInterval.Value.Ticks;

        long sequence = 0;

        foreach (var observer in _orderedAgents)
        {
            if (!_agents.TryGetValue(
                    observer,
                    out var agent))
            {
                continue;
            }

            agent.SelectedSensors.Clear();
            agent.UpdatedSensorNames.Clear();

            ref readonly var ai =
                ref _world.GetReadOnly<AIComponent>(
                    observer);

            if (!ai.Enabled)
                continue;

            foreach (var registration in agent.Sensors)
            {
                if (now < registration.NextUpdateAt)
                    continue;

                long overdueTicks =
                    (now - registration.NextUpdateAt).Ticks;

                long ageBoost =
                    overdueTicks / agingTicks;

                ageBoost = Math.Min(
                    ageBoost,
                    1_000_000L);

                long effectivePriority =
                    (long)registration.Settings.Priority +
                    ageBoost;

                _dueSensors.Add(
                    new DueSensor(
                        observer,
                        agent,
                        registration,
                        effectivePriority,
                        sequence++));
            }
        }

        _dueSensors.Sort(CompareDueSensors);
    }

    private static int CompareDueSensors(
        DueSensor left,
        DueSensor right)
    {
        int comparison =
            right.EffectivePriority.CompareTo(
                left.EffectivePriority);

        if (comparison != 0)
            return comparison;

        comparison =
            left.Registration.LastServedOrder.CompareTo(
                right.Registration.LastServedOrder);

        if (comparison != 0)
            return comparison;

        return left.Sequence.CompareTo(right.Sequence);
    }

    private void SelectSensorsWithinBudget()
    {
        int count = Math.Min(
            Options.MaxSensorUpdatesPerUpdate,
            _dueSensors.Count);

        for (int i = 0; i < count; i++)
        {
            DueSensor due = _dueSensors[i];

            due.Agent.SelectedSensors.Add(
                due.Registration);

            due.Agent.UpdatedSensorNames.Add(
                due.Registration.Name);
        }
    }

    private void UpdateSelectedAgents(
        SystemContext context,
        TimeSpan now)
    {
        for (int i = 0; i < _orderedAgents.Count; i++)
        {
            EntityId observer = _orderedAgents[i];

            if (!_agents.TryGetValue(
                    observer,
                    out var agent))
            {
                continue;
            }

            if (agent.SelectedSensors.Count == 0)
                continue;

            UpdateAgent(
                observer,
                agent,
                context,
                now);
        }
    }

    private void UpdateAgent(
        EntityId observer,
        AgentSensors agent,
        SystemContext context,
        TimeSpan now)
    {
        agent.State.BeginUpdate(
            agent.ActiveSensorNames,
            agent.UpdatedSensorNames);

        var perceptionContext = new PerceptionContext(
            _world,
            observer,
            context.Time);

        try
        {
            foreach (var registration in agent.SelectedSensors)
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
                    registration.Name,
                    now);

                registration.Sensor.Sense(
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

            foreach (var registration in agent.SelectedSensors)
            {
                registration.NextUpdateAt =
                    now + registration.Settings.UpdateInterval.Value;

                registration.LastServedOrder =
                    NextServiceOrder();
            }
        }
        catch
        {
            agent.State.AbortUpdate();
            throw;
        }
    }

    private long NextServiceOrder()
    {
        if (_serviceOrder == long.MaxValue)
        {
            NormalizeServiceOrder();
        }

        return ++_serviceOrder;
    }

    private void NormalizeServiceOrder()
    {
        var registrations = _agents.Values
            .SelectMany(agent => agent.Sensors)
            .OrderBy(registration =>
                registration.LastServedOrder)
            .ThenBy(registration =>
                registration.Name, StringComparer.Ordinal)
            .ToArray();

        long order = 0;

        foreach (var registration in registrations)
        {
            registration.LastServedOrder = ++order;
        }

        _serviceOrder = order;
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
        _dueSensors.Clear();

        _disposed = true;
    }
}
