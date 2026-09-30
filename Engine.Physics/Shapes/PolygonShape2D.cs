using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.Shapes;

public readonly struct PolygonShape2D :
    IDeterministicState,
    IEquatable<PolygonShape2D>
{
    private readonly FixedVector2[] _vertices;

    public PolygonShape2D(
        IReadOnlyList<FixedVector2> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        if (vertices.Count < 3)
        {
            throw new ArgumentException(
                "Polygon must contain at least three vertices.",
                nameof(vertices));
        }

        var copy =
            new FixedVector2[
                vertices.Count];

        for (var i = 0;
             i < vertices.Count;
             i++)
        {
            copy[i] =
                vertices[i];
        }

        ValidateConvexCounterClockwise(
            copy);

        _vertices =
            copy;
    }

    public int VertexCount =>
        _vertices?.Length ?? 0;

    public FixedVector2 GetVertex(
        int index)
    {
        if (_vertices is null)
        {
            throw new InvalidOperationException(
                "Polygon shape is not initialized.");
        }

        return _vertices[index];
    }

    public FixedBounds2 GetBounds(
        FixedVector2 position)
    {
        if (_vertices is null ||
            _vertices.Length == 0)
        {
            throw new InvalidOperationException(
                "Polygon shape is not initialized.");
        }

        var minimum =
            _vertices[0];

        var maximum =
            _vertices[0];

        for (var i = 1;
             i < _vertices.Length;
             i++)
        {
            var vertex =
                _vertices[i];

            minimum =
                new FixedVector2(
                    Fixed32.Min(
                        minimum.X,
                        vertex.X),
                    Fixed32.Min(
                        minimum.Y,
                        vertex.Y));

            maximum =
                new FixedVector2(
                    Fixed32.Max(
                        maximum.X,
                        vertex.X),
                    Fixed32.Max(
                        maximum.Y,
                        vertex.Y));
        }

        return new FixedBounds2(
            minimum + position,
            maximum + position);
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddInt32(
            VertexCount);

        if (_vertices is null)
        {
            return;
        }

        foreach (var vertex in _vertices)
        {
            hasher.AddFixedVector2(
                vertex);
        }
    }

    public bool Equals(
        PolygonShape2D other)
    {
        if (VertexCount != other.VertexCount)
        {
            return false;
        }

        for (var i = 0;
             i < VertexCount;
             i++)
        {
            if (GetVertex(i) !=
                other.GetVertex(i))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(
        object? obj)
    {
        return obj is PolygonShape2D other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        var hash =
            new HashCode();

        hash.Add(
            VertexCount);

        for (var i = 0;
             i < VertexCount;
             i++)
        {
            hash.Add(
                GetVertex(i));
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(
        PolygonShape2D left,
        PolygonShape2D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        PolygonShape2D left,
        PolygonShape2D right)
    {
        return !left.Equals(right);
    }

    private static void ValidateConvexCounterClockwise(
        FixedVector2[] vertices)
    {
        for (var i = 0;
             i < vertices.Length;
             i++)
        {
            var current =
                vertices[i];

            var next =
                vertices[
                    (i + 1) %
                    vertices.Length];

            if (current ==
                next)
            {
                throw new ArgumentException(
                    "Polygon cannot contain duplicate consecutive vertices.",
                    nameof(vertices));
            }
        }

        for (var i = 0;
             i < vertices.Length;
             i++)
        {
            var previous =
                vertices[
                    (i - 1 + vertices.Length) %
                    vertices.Length];

            var current =
                vertices[i];

            var next =
                vertices[
                    (i + 1) %
                    vertices.Length];

            var firstEdge =
                current -
                previous;

            var secondEdge =
                next -
                current;

            var cross =
                Cross(
                    firstEdge,
                    secondEdge);

            if (cross <= Fixed32.Zero)
            {
                throw new ArgumentException(
                    "Polygon vertices must define a strictly convex " +
                    "counter-clockwise polygon.",
                    nameof(vertices));
            }
        }
    }

    private static Fixed32 Cross(
        FixedVector2 left,
        FixedVector2 right)
    {
        return
            left.X * right.Y -
            left.Y * right.X;
    }
}