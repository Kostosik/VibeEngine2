
using Engine.AI.Perception.Core;
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Engine.AI.Perception.Sensors;

public delegate Vector2 ObserverDirectionProvider(
    in PerceptionContext context);

public sealed class FieldOfViewSensor<TComponent> : IPerceptionSensor
    where TComponent : struct
{
    private readonly PerceptionVisibilityTest<TComponent>?
    _visibilityTest;
    private readonly ObserverPositionProvider _observerPosition;
    private readonly ObserverDirectionProvider _observerDirection;

    private readonly TargetPositionProvider<TComponent> _targetPosition;
    private readonly PerceptionTargetFilter<TComponent>? _filter;

    private readonly float _rangeSquared;
    private readonly float _cosHalfAngle;
    private readonly bool _omnidirectional;

    public string Name { get; }

    public float Range { get; }

    public float FieldOfViewDegrees { get; }

    public FieldOfViewSensor(
        string name,
        float range,
        float fieldOfViewDegrees,
        ObserverPositionProvider observerPosition,
        ObserverDirectionProvider observerDirection,
        TargetPositionProvider<TComponent> targetPosition,
        PerceptionTargetFilter<TComponent>? filter = null,
        PerceptionVisibilityTest<TComponent>? visibilityTest = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(observerPosition);
        ArgumentNullException.ThrowIfNull(observerDirection);
        ArgumentNullException.ThrowIfNull(targetPosition);

        if (!float.IsFinite(range) || range <= 0f)
            throw new ArgumentOutOfRangeException(nameof(range));

        float rangeSquared = range * range;

        if (!float.IsFinite(rangeSquared) || rangeSquared <= 0f)
            throw new ArgumentOutOfRangeException(nameof(range));

        if (!float.IsFinite(fieldOfViewDegrees) ||
            fieldOfViewDegrees <= 0f ||
            fieldOfViewDegrees > 360f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fieldOfViewDegrees));
        }

        Name = name;
        Range = range;
        FieldOfViewDegrees = fieldOfViewDegrees;

        _rangeSquared = rangeSquared;
        _cosHalfAngle = MathF.Cos(
            fieldOfViewDegrees * (MathF.PI / 360f));

        _omnidirectional = fieldOfViewDegrees == 360f;

        _observerPosition = observerPosition;
        _observerDirection = observerDirection;
        _targetPosition = targetPosition;
        _filter = filter;
        _visibilityTest = visibilityTest;
    }

    public void Sense(
        in PerceptionContext context,
        PerceptionBuffer results)
    {
        ArgumentNullException.ThrowIfNull(results);

        Vector2 origin = _observerPosition(in context);
        Vector2 direction = _observerDirection(in context);

        if (!IsFinite(origin))
        {
            throw new InvalidOperationException(
                $"Sensor '{Name}' received an invalid observer position.");
        }

        if (!IsFinite(direction))
        {
            throw new InvalidOperationException(
                $"Sensor '{Name}' received an invalid observer direction.");
        }

        float directionLengthSquared =
            direction.X * direction.X +
            direction.Y * direction.Y;

        if (!float.IsFinite(directionLengthSquared) ||
            directionLengthSquared <= 0f)
        {
            throw new InvalidOperationException(
                $"Sensor '{Name}' received a zero observer direction.");
        }

        float inverseDirectionLength =
            1f / MathF.Sqrt(directionLengthSquared);

        float forwardX = direction.X * inverseDirectionLength;
        float forwardY = direction.Y * inverseDirectionLength;

        foreach (var item in context.World.Query<TComponent>())
        {
            if (item.Entity == context.Observer)
                continue;

            TComponent component = item.Component;

            if (_filter != null &&
                !_filter(item.Entity, component))
            {
                continue;
            }

            Vector2 target =
                _targetPosition(item.Entity, component);

            if (!IsFinite(target))
                continue;

            float dx = target.X - origin.X;
            float dy = target.Y - origin.Y;

            float distanceSquared =
                dx * dx + dy * dy;

            if (!float.IsFinite(distanceSquared) ||
                distanceSquared > _rangeSquared)
            {
                continue;
            }

            if (!_omnidirectional && distanceSquared > 0f)
            {
                float inverseDistance =
                    1f / MathF.Sqrt(distanceSquared);

                float dot =
                    (dx * forwardX + dy * forwardY) *
                    inverseDistance;

                if (dot + 0.000001f < _cosHalfAngle)
                    continue;
            }

            float confidence = Math.Clamp(
                1f - distanceSquared / _rangeSquared,
                0f,
                1f);

            if (_visibilityTest != null &&
                !_visibilityTest(
                    in context,
                    item.Entity,
                    component,
                    origin,
                    target))
            {
                continue;
            }
            results.Add(
                item.Entity,
                confidence,
                distanceSquared,
                targetPosition: target);
        }
    }

    private static bool IsFinite(Vector2 value)
    {
        return float.IsFinite(value.X) &&
               float.IsFinite(value.Y);
    }
}
