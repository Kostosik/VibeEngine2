using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.Shapes;

public enum PhysicsShapeType
{
    Aabb = 0,
    Circle = 1,
    Polygon = 2
}

public readonly struct PhysicsShape2D :
    IDeterministicState,
    IEquatable<PhysicsShape2D>
{
    private readonly AabbShape2D _aabb;
    private readonly CircleShape2D _circle;
    private readonly PolygonShape2D _polygon;

    private PhysicsShape2D(
        AabbShape2D aabb)
    {
        Type = PhysicsShapeType.Aabb;
        _aabb = aabb;
        _circle = default;
        _polygon = default;
    }

    private PhysicsShape2D(
        CircleShape2D circle)
    {
        Type = PhysicsShapeType.Circle;
        _aabb = default;
        _circle = circle;
        _polygon = default;
    }

    private PhysicsShape2D(
        PolygonShape2D polygon)
    {
        Type = PhysicsShapeType.Polygon;
        _aabb = default;
        _circle = default;
        _polygon = polygon;
    }

    public PhysicsShapeType Type { get; }

    public FixedVector2 Size =>
        Type switch
        {
            PhysicsShapeType.Aabb =>
                _aabb.Size,

            PhysicsShapeType.Circle =>
                new FixedVector2(
                    _circle.Diameter,
                    _circle.Diameter),

            PhysicsShapeType.Polygon =>
                _polygon
                    .GetBounds(
                        FixedVector2.Zero)
                    .Size,

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{Type}'.")
        };

    public FixedVector2 HalfSize =>
        Size *
        Fixed32.FromRatio(
            1,
            2);

    public static PhysicsShape2D FromAabb(
        AabbShape2D shape)
    {
        return new PhysicsShape2D(
            shape);
    }

    public static PhysicsShape2D FromCircle(
        CircleShape2D shape)
    {
        return new PhysicsShape2D(
            shape);
    }

    public static PhysicsShape2D FromPolygon(
        PolygonShape2D shape)
    {
        return new PhysicsShape2D(
            shape);
    }

    public AabbShape2D Aabb
    {
        get
        {
            EnsureType(
                PhysicsShapeType.Aabb);

            return _aabb;
        }
    }

    public CircleShape2D Circle
    {
        get
        {
            EnsureType(
                PhysicsShapeType.Circle);

            return _circle;
        }
    }

    public PolygonShape2D Polygon
    {
        get
        {
            EnsureType(
                PhysicsShapeType.Polygon);

            return _polygon;
        }
    }

    public FixedBounds2 GetBounds(
        FixedVector2 position)
    {
        return Type switch
        {
            PhysicsShapeType.Aabb =>
                _aabb.GetBounds(position),

            PhysicsShapeType.Circle =>
                _circle.GetBounds(position),

            PhysicsShapeType.Polygon =>
                _polygon.GetBounds(position),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{Type}'.")
        };
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddInt32(
            (int)Type);

        switch (Type)
        {
            case PhysicsShapeType.Aabb:
                _aabb.AddToHash(
                    ref hasher);
                break;

            case PhysicsShapeType.Circle:
                _circle.AddToHash(
                    ref hasher);
                break;

            case PhysicsShapeType.Polygon:
                _polygon.AddToHash(
                    ref hasher);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{Type}'.");
        }
    }

    public bool Equals(
        PhysicsShape2D other)
    {
        if (Type != other.Type)
        {
            return false;
        }

        return Type switch
        {
            PhysicsShapeType.Aabb =>
                _aabb == other._aabb,

            PhysicsShapeType.Circle =>
                _circle == other._circle,

            PhysicsShapeType.Polygon =>
                _polygon == other._polygon,

            _ =>
                false
        };
    }

    public override bool Equals(
        object? obj)
    {
        return obj is PhysicsShape2D other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return Type switch
        {
            PhysicsShapeType.Aabb =>
                HashCode.Combine(
                    Type,
                    _aabb),

            PhysicsShapeType.Circle =>
                HashCode.Combine(
                    Type,
                    _circle),

            PhysicsShapeType.Polygon =>
                HashCode.Combine(
                    Type,
                    _polygon),

            _ =>
                (int)Type
        };
    }

    public static bool operator ==(
        PhysicsShape2D left,
        PhysicsShape2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        PhysicsShape2D left,
        PhysicsShape2D right)
    {
        return !left.Equals(right);
    }

    private void EnsureType(
        PhysicsShapeType expected)
    {
        if (Type != expected)
        {
            throw new InvalidOperationException(
                $"Physics shape is '{Type}', " +
                $"but '{expected}' was requested.");
        }
    }
}