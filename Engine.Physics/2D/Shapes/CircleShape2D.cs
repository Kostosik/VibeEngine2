using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.Shapes;

public readonly struct CircleShape2D :
    IDeterministicState,
    IEquatable<CircleShape2D>
{
    public CircleShape2D(
        Fixed32 radius)
    {
        if (radius <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                "Circle radius must be greater than zero.");
        }

        Radius = radius;
    }

    public Fixed32 Radius { get; }

    public Fixed32 Diameter =>
        Radius *
        Fixed32.FromInt(2);

    public FixedBounds2 GetBounds(
        FixedVector2 position)
    {
        var size =
            new FixedVector2(
                Diameter,
                Diameter);

        return FixedBounds2.FromPositionSize(
            position -
            new FixedVector2(
                Radius,
                Radius),
            size);
    }

    public bool ContainsPoint(
        FixedVector2 center,
        FixedVector2 point)
    {
        var delta =
            point -
            center;

        return delta.Dot(delta) <=
               Radius * Radius;
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixed32(
            Radius);
    }

    public bool Equals(
        CircleShape2D other)
    {
        return Radius == other.Radius;
    }

    public override bool Equals(
        object? obj)
    {
        return obj is CircleShape2D other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return Radius.GetHashCode();
    }

    public static bool operator ==(
        CircleShape2D left,
        CircleShape2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        CircleShape2D left,
        CircleShape2D right)
    {
        return !left.Equals(right);
    }
}