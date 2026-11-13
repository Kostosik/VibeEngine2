using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD.Shapes;

public enum PhysicsShapeType3D
{
    Sphere = 0,
    Box = 1,
    Capsule = 2,
    Plane = 3
}

public readonly struct PhysicsShape3D :
    IDeterministicState,
    IEquatable<PhysicsShape3D>
{
    private readonly SphereShape3D _sphere;
    private readonly BoxShape3D _box;
    private readonly CapsuleShape3D _capsule;
    private readonly PlaneShape3D _plane;

    private PhysicsShape3D(SphereShape3D shape)
    {
        Type = PhysicsShapeType3D.Sphere;
        _sphere = shape;
        _box = default;
        _capsule = default;
        _plane = default;
    }

    private PhysicsShape3D(BoxShape3D shape)
    {
        Type = PhysicsShapeType3D.Box;
        _sphere = default;
        _box = shape;
        _capsule = default;
        _plane = default;
    }

    private PhysicsShape3D(CapsuleShape3D shape)
    {
        Type = PhysicsShapeType3D.Capsule;
        _sphere = default;
        _box = default;
        _capsule = shape;
        _plane = default;
    }

    private PhysicsShape3D(PlaneShape3D shape)
    {
        Type = PhysicsShapeType3D.Plane;
        _sphere = default;
        _box = default;
        _capsule = default;
        _plane = shape;
    }

    public PhysicsShapeType3D Type { get; }

    internal bool IsValid =>
        Type switch
        {
            PhysicsShapeType3D.Sphere =>
                _sphere.Radius > Fixed32.Zero,

            PhysicsShapeType3D.Box =>
                _box.HalfExtents.X > Fixed32.Zero &&
                _box.HalfExtents.Y > Fixed32.Zero &&
                _box.HalfExtents.Z > Fixed32.Zero,

            PhysicsShapeType3D.Capsule =>
                _capsule.Radius > Fixed32.Zero &&
                _capsule.HalfHeight >= Fixed32.Zero,

            PhysicsShapeType3D.Plane =>
                _plane.Normal.LengthSquared() >
                Fixed32.Zero,

            _ => false
        };

    private static PhysicsShape3D Validate(PhysicsShape3D shape)
    {
        if (!shape.IsValid)
        {
            throw new ArgumentException(
                "Physics shape must be initialized and valid.",
                nameof(shape));
        }

        return shape;
    }

    public static PhysicsShape3D FromSphere(SphereShape3D shape) =>
        Validate(new PhysicsShape3D(shape));

    public static PhysicsShape3D FromBox(BoxShape3D shape) =>
        Validate(new PhysicsShape3D(shape));

    public static PhysicsShape3D FromCapsule(CapsuleShape3D shape) =>
        Validate(new PhysicsShape3D(shape));

    public static PhysicsShape3D FromPlane(PlaneShape3D shape) =>
        Validate(new PhysicsShape3D(shape));

    public SphereShape3D Sphere
    {
        get
        {
            EnsureType(PhysicsShapeType3D.Sphere);
            return _sphere;
        }
    }

    public BoxShape3D Box
    {
        get
        {
            EnsureType(PhysicsShapeType3D.Box);
            return _box;
        }
    }

    public CapsuleShape3D Capsule
    {
        get
        {
            EnsureType(PhysicsShapeType3D.Capsule);
            return _capsule;
        }
    }

    public PlaneShape3D Plane
    {
        get
        {
            EnsureType(PhysicsShapeType3D.Plane);
            return _plane;
        }
    }

    public PhysicsBounds3 GetWorldBounds(
        FixedVector3 position,
        FixedQuaternion rotation,
        FixedVector3 scale)
    {
        return Type switch
        {
            PhysicsShapeType3D.Sphere =>
                _sphere.GetBounds(
                    position,
                    GetMaximumScale(scale)),

            PhysicsShapeType3D.Box =>
                _box.GetBounds(
                    position,
                    rotation,
                    scale),

            PhysicsShapeType3D.Capsule =>
                _capsule.GetBounds(
                    position,
                    rotation,
                    scale),

            PhysicsShapeType3D.Plane =>
                _plane.GetBounds(),

            _ => throw new InvalidOperationException(
                $"Unsupported physics shape type '{Type}'.")
        };
    }

    private static Fixed32 GetMaximumScale(FixedVector3 scale)
    {
        return Fixed32.Max(
            Fixed32.Abs(scale.X),
            Fixed32.Max(
                Fixed32.Abs(scale.Y),
                Fixed32.Abs(scale.Z)));
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddInt32((int)Type);

        switch (Type)
        {
            case PhysicsShapeType3D.Sphere:
                _sphere.AddToHash(ref hasher);
                break;

            case PhysicsShapeType3D.Box:
                _box.AddToHash(ref hasher);
                break;

            case PhysicsShapeType3D.Capsule:
                _capsule.AddToHash(ref hasher);
                break;

            case PhysicsShapeType3D.Plane:
                _plane.AddToHash(ref hasher);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{Type}'.");
        }
    }

    public bool Equals(PhysicsShape3D other)
    {
        if (Type != other.Type)
            return false;

        return Type switch
        {
            PhysicsShapeType3D.Sphere =>
                _sphere == other._sphere,

            PhysicsShapeType3D.Box =>
                _box == other._box,

            PhysicsShapeType3D.Capsule =>
                _capsule == other._capsule,

            PhysicsShapeType3D.Plane =>
                _plane == other._plane,

            _ => false
        };
    }

    public override bool Equals(object? obj) =>
        obj is PhysicsShape3D other && Equals(other);

    public override int GetHashCode()
    {
        return Type switch
        {
            PhysicsShapeType3D.Sphere => HashCode.Combine(Type, _sphere),
            PhysicsShapeType3D.Box => HashCode.Combine(Type, _box),
            PhysicsShapeType3D.Capsule => HashCode.Combine(Type, _capsule),
            PhysicsShapeType3D.Plane => HashCode.Combine(Type, _plane),
            _ => (int)Type
        };
    }

    public static bool operator ==(
        PhysicsShape3D left,
        PhysicsShape3D right) => left.Equals(right);

    public static bool operator !=(
        PhysicsShape3D left,
        PhysicsShape3D right) => !left.Equals(right);

    private void EnsureType(PhysicsShapeType3D expected)
    {
        if (Type != expected)
        {
            throw new InvalidOperationException(
                $"Physics shape is '{Type}', but '{expected}' was requested.");
        }
    }
}