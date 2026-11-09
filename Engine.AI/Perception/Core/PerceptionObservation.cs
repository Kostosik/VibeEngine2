using Engine.ECS.Entities;

namespace Engine.AI.Perception.Core;

public readonly record struct PerceptionObservation(
    EntityId Target,
    string SensorName,
    float Confidence,
    float DistanceSquared,
    TimeSpan ObservedAt);