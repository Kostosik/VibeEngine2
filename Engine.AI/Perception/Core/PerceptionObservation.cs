
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Engine.AI.Perception.Core;

public readonly record struct PerceptionObservation(
    EntityId Target,
    string SensorName,
    float Confidence,
    float DistanceSquared,
    TimeSpan ObservedAt)
{
    /// <summary>
    /// Target position at the moment of observation.
    /// Null when the sensor does not provide a position.
    /// </summary>
    public Vector2? TargetPosition { get; init; }
}
