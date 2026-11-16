using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Collision;

public readonly record struct ContactPoint3D(
    FixedVector3 Position,
    FixedVector3 Normal,
    Fixed32 Penetration);