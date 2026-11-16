namespace Engine.Physics.ThreeD.Collision;

public readonly record struct CollisionManifold3D(
    CollisionPair3D Pair,
    ContactPoint3D Contact);