using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Physics.Materials;
using Engine.Physics.Shapes;

namespace Engine.Physics.Components;

public struct Collider2D :
    IDeterministicState
{
    public Collider2D(
        AabbShape2D shape)
    {
        Shape = shape;
        Offset = FixedVector2.Zero;
        Material = PhysicsMaterial2D.Default;
        IsTrigger = false;
        Enabled = true;
        CollisionLayer = 1u;
        CollisionMask = uint.MaxValue;
    }

    public AabbShape2D Shape { get; set; }

    public FixedVector2 Offset { get; set; }

    public PhysicsMaterial2D Material { get; set; }

    public bool IsTrigger { get; set; }

    public bool Enabled { get; set; }

    public uint CollisionLayer { get; set; }

    public uint CollisionMask { get; set; }

    public FixedBounds2 GetWorldBounds(
        FixedVector2 bodyPosition)
    {
        return Shape.GetBounds(
            bodyPosition + Offset);
    }

    public bool CanCollideWith(
        Collider2D other)
    {
        return
            (CollisionMask & other.CollisionLayer) != 0 &&
            (other.CollisionMask & CollisionLayer) != 0;
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        Shape.AddToHash(ref hasher);

        hasher.AddFixedVector2(
            Offset);

        Material.AddToHash(
            ref hasher);

        hasher.AddBool(
            IsTrigger);

        hasher.AddBool(
            Enabled);

        hasher.AddUInt32(
            CollisionLayer);

        hasher.AddUInt32(
            CollisionMask);
    }
}