using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Components;

public enum PhysicsBodyType3D
{
    Static = 0,
    Dynamic = 1,
    Kinematic = 2
}

public struct PhysicsBody3D : IDeterministicState
{
    private PhysicsBodyType3D _bodyType;
    private bool _isSleeping;
    private Fixed32 _sleepTimer;
    private Fixed32 _mass;
    private FixedVector3 _inertia;
    private Fixed32 _linearDamping;
    private Fixed32 _angularDamping;

    public PhysicsBody3D(
        PhysicsBodyType3D bodyType,
        Fixed32 mass)
    {
        if (!Enum.IsDefined(typeof(PhysicsBodyType3D), bodyType))
        {
            throw new ArgumentOutOfRangeException(nameof(bodyType));
        }

        if (bodyType == PhysicsBodyType3D.Dynamic &&
            mass <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mass),
                "Dynamic body mass must be greater than zero.");
        }

        if (mass <= Fixed32.Zero)
            mass = Fixed32.One;

        _bodyType = bodyType;
        _isSleeping = false;
        _sleepTimer = Fixed32.Zero;
        _mass = mass;
        _inertia = FixedVector3.One;
        _linearDamping = Fixed32.Zero;
        _angularDamping = Fixed32.Zero;

        LinearVelocity = FixedVector3.Zero;
        AngularVelocity = FixedVector3.Zero;
        Force = FixedVector3.Zero;
        Torque = FixedVector3.Zero;
        GravityScale = Fixed32.One;
    }

    public PhysicsBodyType3D BodyType
    {
        readonly get => _bodyType;
        set
        {
            if (!Enum.IsDefined(typeof(PhysicsBodyType3D), value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _bodyType = value;

            if (value != PhysicsBodyType3D.Dynamic)
                WakeUp();
        }
    }

    public FixedVector3 LinearVelocity { get; set; }

    public FixedVector3 AngularVelocity { get; set; }

    public FixedVector3 Force { get; private set; }

    public FixedVector3 Torque { get; private set; }

    public Fixed32 Mass
    {
        readonly get => _mass;
        set => SetMass(value);
    }

    // Diagonal inertia tensor in local body space.
    public FixedVector3 Inertia
    {
        readonly get => _inertia;
        set => SetInertia(value);
    }

    public Fixed32 GravityScale { get; set; }

    public Fixed32 LinearDamping
    {
        readonly get => _linearDamping;
        set
        {
            if (value < Fixed32.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));

            _linearDamping = value;
        }
    }

    public Fixed32 AngularDamping
    {
        readonly get => _angularDamping;
        set
        {
            if (value < Fixed32.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));

            _angularDamping = value;
        }
    }

    public bool IsSleeping
    {
        readonly get => _isSleeping;
        set => _isSleeping = value;
    }

    public Fixed32 SleepTimer
    {
        readonly get => _sleepTimer;
        set => _sleepTimer = value;
    }

    public readonly Fixed32 InverseMass =>
        BodyType == PhysicsBodyType3D.Dynamic
            ? Fixed32.One / Mass
            : Fixed32.Zero;

    public readonly FixedVector3 InverseInertia
    {
        get
        {
            if (BodyType != PhysicsBodyType3D.Dynamic)
                return FixedVector3.Zero;

            return new FixedVector3(
                Fixed32.One / Inertia.X,
                Fixed32.One / Inertia.Y,
                Fixed32.One / Inertia.Z);
        }
    }

    public void SetMass(Fixed32 mass)
    {
        if (mass <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mass),
                "Mass must be greater than zero.");
        }

        _mass = mass;
    }

    public void SetInertia(FixedVector3 inertia)
    {
        if (inertia.X <= Fixed32.Zero ||
            inertia.Y <= Fixed32.Zero ||
            inertia.Z <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inertia),
                "All inertia components must be greater than zero.");
        }

        _inertia = inertia;
    }

    public void WakeUp()
    {
        _isSleeping = false;
        _sleepTimer = Fixed32.Zero;
    }

    public void Sleep()
    {
        if (BodyType != PhysicsBodyType3D.Dynamic)
            return;

        _isSleeping = true;
        _sleepTimer = Fixed32.Zero;

        LinearVelocity = FixedVector3.Zero;
        AngularVelocity = FixedVector3.Zero;
    }

    public void AddForce(FixedVector3 force)
    {
        if (force != FixedVector3.Zero)
            WakeUp();

        Force += force;
    }

    public void AddTorque(FixedVector3 torque)
    {
        if (torque != FixedVector3.Zero)
            WakeUp();

        Torque += torque;
    }

    public void ClearForces()
    {
        Force = FixedVector3.Zero;
        Torque = FixedVector3.Zero;
    }

    public static PhysicsBody3D Dynamic(Fixed32 mass)
    {
        return new PhysicsBody3D(
            PhysicsBodyType3D.Dynamic,
            mass);
    }

    public static PhysicsBody3D Dynamic(
        Fixed32 mass,
        FixedVector3 inertia)
    {
        var body = Dynamic(mass);
        body.SetInertia(inertia);
        return body;
    }

    public static PhysicsBody3D Static()
    {
        return new PhysicsBody3D(
            PhysicsBodyType3D.Static,
            Fixed32.One);
    }

    public static PhysicsBody3D Kinematic()
    {
        return new PhysicsBody3D(
            PhysicsBodyType3D.Kinematic,
            Fixed32.One);
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddInt32((int)BodyType);

        hasher.AddFixedVector3(LinearVelocity);
        hasher.AddFixedVector3(AngularVelocity);
        hasher.AddFixedVector3(Force);
        hasher.AddFixedVector3(Torque);

        hasher.AddFixed32(Mass);
        hasher.AddFixedVector3(Inertia);
        hasher.AddFixed32(GravityScale);
        hasher.AddFixed32(LinearDamping);
        hasher.AddFixed32(AngularDamping);

        hasher.AddBool(IsSleeping);
        hasher.AddFixed32(SleepTimer);
    }
}