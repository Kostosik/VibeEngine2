using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.Components;

public enum PhysicsBodyType
{
    Static = 0,
    Dynamic = 1,
    Kinematic = 2
}

public struct PhysicsBody2D :
    IDeterministicState
{
    public PhysicsBody2D(
        PhysicsBodyType bodyType,
        Fixed32 mass)
    {
        if (bodyType == PhysicsBodyType.Dynamic &&
            mass <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mass),
                "Dynamic body mass must be greater than zero.");
        }

        if (mass <= Fixed32.Zero)
        {
            mass = Fixed32.One;
        }

        BodyType = bodyType;
        Velocity = FixedVector2.Zero;
        Force = FixedVector2.Zero;
        Mass = mass;
        GravityScale = Fixed32.One;
        AngularVelocity = Fixed32.Zero;
    }

    public PhysicsBodyType BodyType { get; set; }

    public FixedVector2 Velocity { get; set; }

    public FixedVector2 Force { get; private set; }

    public Fixed32 Mass { get; private set; }

    public Fixed32 GravityScale { get; set; }

    public Fixed32 AngularVelocity { get; set; }

    public Fixed32 InverseMass =>
        BodyType == PhysicsBodyType.Dynamic
            ? Fixed32.One / Mass
            : Fixed32.Zero;

    public static PhysicsBody2D Dynamic(
        Fixed32 mass)
    {
        return new(
            PhysicsBodyType.Dynamic,
            mass);
    }

    public static PhysicsBody2D Static()
    {
        return new(
            PhysicsBodyType.Static,
            Fixed32.One);
    }

    public static PhysicsBody2D Kinematic()
    {
        return new(
            PhysicsBodyType.Kinematic,
            Fixed32.One);
    }

    public void AddForce(
        FixedVector2 force)
    {
        Force += force;
    }

    public void ClearForces()
    {
        Force = FixedVector2.Zero;
    }

    public void SetMass(
        Fixed32 mass)
    {
        if (mass <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mass),
                "Mass must be greater than zero.");
        }

        Mass = mass;
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddInt32(
            (int)BodyType);

        hasher.AddFixedVector2(
            Velocity);

        hasher.AddFixedVector2(
            Force);

        hasher.AddFixed32(
            Mass);

        hasher.AddFixed32(
            GravityScale);

        hasher.AddFixed32(
            AngularVelocity);
    }
}