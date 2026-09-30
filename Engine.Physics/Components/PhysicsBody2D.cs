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
        Inertia = Fixed32.One;
        GravityScale = Fixed32.One;
        AngularVelocity = Fixed32.Zero;
        Torque = Fixed32.Zero;
        LinearDamping = Fixed32.Zero;
        AngularDamping = Fixed32.Zero;
    }
    public PhysicsBodyType BodyType { get; set; }

    public FixedVector2 Velocity { get; set; }

    public FixedVector2 Force { get; private set; }

    public Fixed32 Mass { get; private set; }

    public Fixed32 Inertia { get; private set; }

    public Fixed32 GravityScale { get; set; }

    public Fixed32 AngularVelocity { get; set; }

    public Fixed32 Torque { get; private set; }

    public Fixed32 InverseMass =>
        BodyType == PhysicsBodyType.Dynamic
            ? Fixed32.One / Mass
            : Fixed32.Zero;

    public Fixed32 InverseInertia =>
        BodyType == PhysicsBodyType.Dynamic
            ? Fixed32.One / Inertia
            : Fixed32.Zero;

    public static PhysicsBody2D Dynamic(
        Fixed32 mass)
    {
        return new(
            PhysicsBodyType.Dynamic,
            mass);
    }

    public static PhysicsBody2D Dynamic(
        Fixed32 mass,
        Fixed32 inertia)
    {
        var body =
            new PhysicsBody2D(
                PhysicsBodyType.Dynamic,
                mass);

        body.SetInertia(
            inertia);

        return body;
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

    public void AddTorque(
        Fixed32 torque)
    {
        Torque += torque;
    }

    public void ClearForces()
    {
        Force = FixedVector2.Zero;
        Torque = Fixed32.Zero;
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
    private Fixed32 _linearDamping;
    private Fixed32 _angularDamping;
    public Fixed32 LinearDamping
    {
        get => _linearDamping;
        set
        {
            if (value < Fixed32.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Linear damping cannot be negative.");
            }

            _linearDamping = value;
        }
    }

    public Fixed32 AngularDamping
    {
        get => _angularDamping;
        set
        {
            if (value < Fixed32.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Angular damping cannot be negative.");
            }

            _angularDamping = value;
        }
    }

    public void SetInertia(
        Fixed32 inertia)
    {
        if (inertia <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inertia),
                "Inertia must be greater than zero.");
        }

        Inertia = inertia;
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
            Inertia);

        hasher.AddFixed32(
            GravityScale);

        hasher.AddFixed32(
            AngularVelocity);

        hasher.AddFixed32(
            Torque);
    }
}