namespace Engine.Physics.Collision;

public readonly record struct CollisionManifold(
    CollisionPair Pair,
    ContactPoint Contact);