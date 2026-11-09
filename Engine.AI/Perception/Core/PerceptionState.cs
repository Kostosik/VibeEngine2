
using System.Collections.ObjectModel;
using Engine.ECS.Entities;

namespace Engine.AI.Perception.Core;

public sealed class PerceptionState
{
    private List<PerceptionObservation> _current = new();

    private List<PerceptionObservation> _pending = new();

    private ReadOnlyCollection<PerceptionObservation>
        _currentView;

    private ReadOnlyCollection<PerceptionObservation>
        _pendingView;

    public IReadOnlyList<PerceptionObservation>
        Observations => _currentView;

    public ulong Version { get; private set; }

    public TimeSpan LastUpdatedAt { get; private set; }

    public bool HasUpdated { get; private set; }

    public PerceptionState()
    {
        _currentView = _current.AsReadOnly();
        _pendingView = _pending.AsReadOnly();
    }

    internal void BeginUpdate(
        IReadOnlySet<string> activeSensorNames,
        IReadOnlySet<string> updatedSensorNames)
    {
        _pending.Clear();

        // Preserve observations from active sensors that
        // are not scheduled for this update.
        foreach (var observation in _current)
        {
            if (!activeSensorNames.Contains(
                    observation.SensorName))
            {
                continue;
            }

            if (updatedSensorNames.Contains(
                    observation.SensorName))
            {
                continue;
            }

            _pending.Add(observation);
        }
    }

    internal void AddPending(
        PerceptionObservation observation)
    {
        _pending.Add(observation);
    }

    internal void CommitUpdate(TimeSpan now)
    {
        (_current, _pending) = (_pending, _current);

        (_currentView, _pendingView) =
            (_pendingView, _currentView);

        LastUpdatedAt = now;
        HasUpdated = true;

        if (Version < ulong.MaxValue)
        {
            Version++;
        }
    }

    internal void AbortUpdate()
    {
        _pending.Clear();
    }

    internal void RemoveSensor(string sensorName)
    {
        int removed = _current.RemoveAll(
            observation =>
                string.Equals(
                    observation.SensorName,
                    sensorName,
                    StringComparison.Ordinal));

        _pending.RemoveAll(
            observation =>
                string.Equals(
                    observation.SensorName,
                    sensorName,
                    StringComparison.Ordinal));

        if (removed > 0 && Version < ulong.MaxValue)
        {
            Version++;
        }
    }

    internal void Clear()
    {
        _current.Clear();
        _pending.Clear();

        LastUpdatedAt = TimeSpan.Zero;
        HasUpdated = false;
        Version = 0;
    }

    public bool TryGetBestObservation(
        EntityId target,
        out PerceptionObservation observation)
    {
        bool found = false;

        observation = default;

        foreach (var item in _current)
        {
            if (item.Target != target)
                continue;

            if (found && item.Confidence <= observation.Confidence)
                continue;

            observation = item;
            found = true;
        }

        return found;
    }

    public int CountObservations(EntityId target)
    {
        int count = 0;

        foreach (var observation in _current)
        {
            if (observation.Target == target)
            {
                count++;
            }
        }

        return count;
    }
}
