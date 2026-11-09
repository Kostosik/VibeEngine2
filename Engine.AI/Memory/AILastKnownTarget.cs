
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Engine.AI.Memory;

public readonly record struct AILastKnownTarget(
    EntityId Target,
    Vector2 Position,
    float Confidence,
    string SensorName,
    TimeSpan LastSeenAt);
