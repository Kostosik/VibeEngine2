
using Engine.AI.Perception.Core;
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Engine.AI.Perception.Sensors;

public delegate Vector2 ObserverPositionProvider(
    in PerceptionContext context);

public delegate Vector2 TargetPositionProvider<TComponent>(
    EntityId entity,
    TComponent component)
    where TComponent : struct;

public delegate bool PerceptionTargetFilter<TComponent>(
    EntityId entity,
    TComponent component)
    where TComponent : struct;

public sealed class RadiusSensor<TComponent> : IPerceptionSensor
    where TComponent : struct
{
    private readonly ObserverPositionProvider _observerPosition;

    private readonly TargetPositionProvider<TComponent>
        _targetPosition;

    private readonly PerceptionTargetFilter<TComponent>?
        _filter;

    private readonly PerceptionVisibilityTest<TComponent>?
        _visibilityTest;

    private readonly float _range;

    private readonly float _rangeSquared;

    public string Name { get; }

    public float Range => _range;

    public RadiusSensor(
        string name,
        float range,
        ObserverPositionProvider observerPosition,
        TargetPositionProvider<TComponent> targetPosition,
        PerceptionTargetFilter<TComponent>? filter = null,
        PerceptionVisibilityTest<TComponent>? visibilityTest = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ArgumentNullException.ThrowIfNull(observerPosition);
        ArgumentNullException.ThrowIfNull(targetPosition);

        if (!float.IsFinite(range) || range <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(range));
        }

        float rangeSquared = range * range;

        if (!float.IsFinite(rangeSquared) ||
            rangeSquared <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(range),
                "Range is outside the supported numeric range.");
        }

        Name = name;

        _range = range;
        _rangeSquared = rangeSquared;

        _observerPosition = observerPosition;
        _targetPosition = targetPosition;
        _filter = filter;
        _visibilityTest = visibilityTest;
    }

    public void Sense(
        in PerceptionContext context,
        PerceptionBuffer results)
    {
        ArgumentNullException.ThrowIfNull(results);

        Vector2 origin =
            _observerPosition(in context);

        if (!IsFinite(origin))
        {
            throw new InvalidOperationException(
                $"Sensor '{Name}' received an invalid observer position.");
        }

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

            Vector2 difference = target - origin;

            float distanceSquared = difference.LengthSquared();

            if (!float.IsFinite(distanceSquared) ||
                distanceSquared > _rangeSquared)
            {
                continue;
            }

            // Expensive visibility checks happen only after
            // the target has passed the range check.
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

            float confidence = Math.Clamp(
                1f - distanceSquared / _rangeSquared,
                0f,
                1f);

            results.Add(
                item.Entity,
                confidence,
                distanceSquared);
        }
    }

    private static bool IsFinite(Vector2 position)
    {
        return float.IsFinite(position.X) &&
               float.IsFinite(position.Y);
    }
}
