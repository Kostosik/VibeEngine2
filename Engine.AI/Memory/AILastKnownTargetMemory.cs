
using Engine.AI.Perception.Core;
using Engine.Core.Time;
using Engine.ECS.Entities;

namespace Engine.AI.Memory;

public sealed class AILastKnownTargetMemory
{
    private readonly Dictionary<EntityId, AILastKnownTarget>
        _targets = new();

    private readonly List<EntityId> _expiredTargets = new();

    private Duration _retention = Duration.FromSeconds(30);

    /// <summary>
    /// How long a target remains remembered after last observation.
    /// </summary>
    public Duration Retention
    {
        get => _retention;
        set
        {
            if (value.IsNegative)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _retention = value;
        }
    }

    public int Count => _targets.Count;

    public void Observe(
        PerceptionState perception,
        TimeSpan now)
    {
        ArgumentNullException.ThrowIfNull(perception);

        foreach (var observation in perception.Observations)
        {
            if (observation.TargetPosition is not { } position)
                continue;

            var remembered = new AILastKnownTarget(
                observation.Target,
                position,
                observation.Confidence,
                observation.SensorName,
                observation.ObservedAt);

            if (_targets.TryGetValue(
                    observation.Target,
                    out var previous))
            {
                if (previous.LastSeenAt > remembered.LastSeenAt)
                    continue;

                if (previous.LastSeenAt == remembered.LastSeenAt &&
                    previous.Confidence >= remembered.Confidence)
                {
                    continue;
                }
            }

            _targets[observation.Target] = remembered;
        }

        PruneExpired(now);
    }

    public bool TryGet(
        EntityId target,
        TimeSpan now,
        out AILastKnownTarget remembered)
    {
        PruneExpired(now);

        return _targets.TryGetValue(
            target,
            out remembered);
    }

    public int PruneExpired(TimeSpan now)
    {
        _expiredTargets.Clear();

        foreach (var pair in _targets)
        {
            TimeSpan age = now - pair.Value.LastSeenAt;

            if (age >= _retention.Value)
            {
                _expiredTargets.Add(pair.Key);
            }
        }

        foreach (var target in _expiredTargets)
        {
            _targets.Remove(target);
        }

        int removed = _expiredTargets.Count;
        _expiredTargets.Clear();

        return removed;
    }

    public bool Remove(EntityId target)
    {
        return _targets.Remove(target);
    }

    public void Clear()
    {
        _targets.Clear();
        _expiredTargets.Clear();
    }
}
