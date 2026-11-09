using Engine.ECS.Entities;

namespace Engine.AI.Perception.Core;

public sealed class PerceptionBuffer
{
    private readonly PerceptionState _state;

    private string _sensorName = string.Empty;

    private TimeSpan _observedAt;

    internal PerceptionBuffer(PerceptionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _state = state;
    }

    internal void BeginSensor(
        string sensorName,
        TimeSpan observedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorName);

        _sensorName = sensorName;
        _observedAt = observedAt;
    }

    public void Add(
        EntityId target,
        float confidence = 1f,
        float distanceSquared = 0f)
    {
        if (!target.IsValid)
        {
            throw new ArgumentException(
                "Target entity is invalid.",
                nameof(target));
        }

        if (!float.IsFinite(confidence) ||
            confidence < 0f ||
            confidence > 1f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence));
        }

        if (!float.IsFinite(distanceSquared) ||
            distanceSquared < 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distanceSquared));
        }

        _state.AddPending(
            new PerceptionObservation(
                target,
                _sensorName,
                confidence,
                distanceSquared,
                _observedAt));
    }
}