using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Physics.ThreeD.Materials;
using Engine.Physics.ThreeD.Shapes;
using Engine.Transform;

namespace Engine.Physics.ThreeD.Components;

public struct Collider3D : IDeterministicState
{
    private PhysicsShape3D _shape;
    private FixedQuaternion _rotation;

    public Collider3D(PhysicsShape3D shape)
    {
        _shape = ValidateShape(shape);

        Offset = FixedVector3.Zero;
        _rotation = FixedQuaternion.Identity;

        Material = PhysicsMaterial3D.Default;

        IsTrigger = false;
        Enabled = true;

        CollisionLayer = 1u;
        CollisionMask = uint.MaxValue;
    }

    public Collider3D(SphereShape3D shape)
        : this(PhysicsShape3D.FromSphere(shape))
    {
    }

    public Collider3D(BoxShape3D shape)
        : this(PhysicsShape3D.FromBox(shape))
    {
    }

    public Collider3D(CapsuleShape3D shape)
        : this(PhysicsShape3D.FromCapsule(shape))
    {
    }

    public Collider3D(PlaneShape3D shape)
        : this(PhysicsShape3D.FromPlane(shape))
    {
    }

    public PhysicsShape3D Shape
    {
        readonly get => _shape;
        set => _shape = ValidateShape(value);
    }

    public FixedVector3 Offset { get; set; }

    public FixedQuaternion Rotation
    {
        readonly get => _rotation;
        set => _rotation = value.Normalize();
    }

    public PhysicsMaterial3D Material { get; set; }

    public bool IsTrigger { get; set; }

    public bool Enabled { get; set; }

    public uint CollisionLayer { get; set; }

    public uint CollisionMask { get; set; }

    private static PhysicsShape3D ValidateShape(
        PhysicsShape3D shape)
    {
        if (!shape.IsValid)
        {
            throw new ArgumentException(
                "Collider shape must be initialized and valid.",
                nameof(shape));
        }

        return shape;
    }

    internal readonly void Validate()
    {
        if (!_shape.IsValid)
        {
            throw new InvalidOperationException(
                "Collider3D has an uninitialized or invalid shape.");
        }
    }

    public readonly FixedVector3 GetWorldPosition(
        FixedTransform3D bodyTransform)
    {
        var scaledOffset = Multiply(
            Offset,
            bodyTransform.Scale);

        return bodyTransform.Position +
               bodyTransform.Rotation.Normalize().Transform(
                   scaledOffset);
    }

    public readonly FixedQuaternion GetWorldRotation(
        FixedTransform3D bodyTransform)
    {
        return (
            bodyTransform.Rotation * Rotation
        ).Normalize();
    }

    public readonly PhysicsBounds3 GetWorldBounds(
        FixedTransform3D bodyTransform)
    {
        return Shape.GetWorldBounds(
            GetWorldPosition(bodyTransform),
            GetWorldRotation(bodyTransform),
            bodyTransform.Scale);
    }

    public readonly bool CanCollideWith(Collider3D other)
    {
        return
            (CollisionMask & other.CollisionLayer) != 0 &&
            (other.CollisionMask & CollisionLayer) != 0;
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        Shape.AddToHash(ref hasher);

        hasher.AddFixedVector3(Offset);
        hasher.AddFixedQuaternion(Rotation);

        Material.AddToHash(ref hasher);

        hasher.AddBool(IsTrigger);
        hasher.AddBool(Enabled);

        hasher.AddUInt32(CollisionLayer);
        hasher.AddUInt32(CollisionMask);
    }

    private static FixedVector3 Multiply(
        FixedVector3 left,
        FixedVector3 right)
    {
        return new FixedVector3(
            left.X * right.X,
            left.Y * right.Y,
            left.Z * right.Z);
    }
}