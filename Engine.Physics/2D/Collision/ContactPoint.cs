using Engine.Core.Math;

namespace Engine.Physics.Collision;

public readonly record struct ContactPoint(
    FixedVector2 Position,
    FixedVector2 Normal,
    Fixed32 Penetration);