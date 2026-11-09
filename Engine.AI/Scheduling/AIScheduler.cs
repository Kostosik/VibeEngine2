using Engine.AI.Components;
using Engine.Core.Time;
using Engine.ECS.Entities;

namespace Engine.AI.Scheduling;

public sealed class AIScheduler
{
    private sealed class AgentSchedule
    {
        public AIUpdateSettings Settings;

        public TimeSpan NextUpdateAt;

        public long LastServedOrder;
    }

    private sealed class Candidate
    {
        public EntityId Entity { get; }

        public AgentSchedule Schedule { get; }

        public long EffectivePriority { get; }

        public Candidate(
            EntityId entity,
            AgentSchedule schedule,
            long effectivePriority)
        {
            Entity = entity;
            Schedule = schedule;
            EffectivePriority = effectivePriority;
        }
    }

    private readonly Dictionary<EntityId, AgentSchedule>
        _agents = new();

    private readonly List<Candidate>
        _candidates = new();

    private readonly List<EntityId>
        _dueAgents = new();

    private readonly HashSet<EntityId>
        _seenAgents = new();

    private readonly IReadOnlyList<EntityId>
        _dueAgentsView;

    private long _serviceOrder;

    public AISchedulerOptions Options { get; }

    public int AgentCount => _agents.Count;

    public AIScheduler(
        AISchedulerOptions? options = null)
    {
        Options = options ?? new AISchedulerOptions();

        _dueAgentsView = _dueAgents.AsReadOnly();
    }

    public bool IsRegistered(EntityId entity)
    {
        return _agents.ContainsKey(entity);
    }

    public bool RegisterOrUpdate(
        EntityId entity,
        AIUpdateSettings settings,
        TimeSpan now)
    {
        ValidateSettings(settings);

        if (_agents.TryGetValue(
                entity,
                out var existing))
        {
            if (existing.Settings == settings)
                return false;

            existing.Settings = settings;
            existing.NextUpdateAt = now;
            existing.LastServedOrder = NextServiceOrder();

            return true;
        }

        var schedule = new AgentSchedule
        {
            Settings = settings,
            NextUpdateAt = now,
            LastServedOrder = NextServiceOrder()
        };

        if (Options.StaggerInitialUpdates)
        {
            schedule.NextUpdateAt +=
                CalculateInitialOffset(entity, settings);
        }

        _agents.Add(entity, schedule);

        return true;
    }

    public bool Unregister(EntityId entity)
    {
        return _agents.Remove(entity);
    }

    public void Clear()
    {
        _agents.Clear();
        _candidates.Clear();
        _dueAgents.Clear();
        _seenAgents.Clear();

        _serviceOrder = 0;
    }

    public IReadOnlyList<EntityId> CollectDueAgents(
        TimeSpan now,
        IReadOnlyList<EntityId> eligibleAgents)
    {
        ArgumentNullException.ThrowIfNull(eligibleAgents);

        _candidates.Clear();
        _dueAgents.Clear();
        _seenAgents.Clear();

        long agingTicks =
            Options.PriorityAgingInterval.Value.Ticks;

        foreach (var entity in eligibleAgents)
        {
            if (!_seenAgents.Add(entity))
                continue;

            if (!_agents.TryGetValue(
                    entity,
                    out var schedule))
            {
                continue;
            }

            if (now < schedule.NextUpdateAt)
                continue;

            long overdueTicks =
                (now - schedule.NextUpdateAt).Ticks;

            long ageBoost =
                overdueTicks / agingTicks;

            // Prevent integer overflow in extreme simulations.
            ageBoost = Math.Min(ageBoost, 1_000_000L);

            long effectivePriority =
                (long)schedule.Settings.Priority + ageBoost;

            _candidates.Add(
                new Candidate(
                    entity,
                    schedule,
                    effectivePriority));
        }

        _candidates.Sort(CompareCandidates);

        int count = Math.Min(
            Options.MaxAgentsPerUpdate,
            _candidates.Count);

        for (int i = 0; i < count; i++)
        {
            _dueAgents.Add(
                _candidates[i].Entity);
        }

        return _dueAgentsView;
    }

    public bool MarkUpdated(
        EntityId entity,
        TimeSpan now)
    {
        if (!_agents.TryGetValue(
                entity,
                out var schedule))
        {
            return false;
        }

        Duration interval = ResolveInterval(
            schedule.Settings);

        schedule.NextUpdateAt =
            now + interval.Value;

        schedule.LastServedOrder =
            NextServiceOrder();

        return true;
    }

    public bool TryGetNextUpdateTime(
        EntityId entity,
        out TimeSpan nextUpdateTime)
    {
        if (_agents.TryGetValue(
                entity,
                out var schedule))
        {
            nextUpdateTime = schedule.NextUpdateAt;
            return true;
        }

        nextUpdateTime = default;
        return false;
    }

    private Duration ResolveInterval(
        AIUpdateSettings settings)
    {
        return settings.UpdateInterval.IsZero
            ? Options.GetInterval(settings.Group)
            : settings.UpdateInterval;
    }

    private static void ValidateSettings(
        AIUpdateSettings settings)
    {
        if (!Enum.IsDefined(settings.Group))
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                "Unknown AI update group.");
        }

        if (settings.UpdateInterval.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                "Update interval cannot be negative.");
        }
    }

    private static int CompareCandidates(
        Candidate left,
        Candidate right)
    {
        int comparison =
            right.EffectivePriority.CompareTo(
                left.EffectivePriority);

        if (comparison != 0)
            return comparison;

        comparison =
            left.Schedule.LastServedOrder.CompareTo(
                right.Schedule.LastServedOrder);

        if (comparison != 0)
            return comparison;

        comparison =
            left.Entity.Index.CompareTo(
                right.Entity.Index);

        if (comparison != 0)
            return comparison;

        return left.Entity.Generation.CompareTo(
            right.Entity.Generation);
    }

    private TimeSpan CalculateInitialOffset(
        EntityId entity,
        AIUpdateSettings settings)
    {
        Duration interval = ResolveInterval(settings);

        long ticks = interval.Value.Ticks;

        if (ticks <= 1)
            return TimeSpan.Zero;

        // Stable hash: no Random or platform-dependent randomness.
        ulong hash =
            ((ulong)entity.Index * 0x9E3779B97F4A7C15UL) ^
            ((ulong)entity.Generation * 0xBF58476D1CE4E5B9UL);

        long offset = (long)(hash % (ulong)ticks);

        return TimeSpan.FromTicks(offset);
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
        var ordered = _agents
            .OrderBy(pair => pair.Value.LastServedOrder)
            .ThenBy(pair => pair.Key.Index)
            .ThenBy(pair => pair.Key.Generation)
            .ToArray();

        long order = 0;

        foreach (var pair in ordered)
        {
            pair.Value.LastServedOrder = ++order;
        }

        _serviceOrder = order;
    }
}