using Engine.Core.Math;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Convex polygon used to clip rendered geometry.
/// Vertices must form a convex polygon.
/// </summary>
public sealed class ClipRegion
{
    private const float Epsilon = 0.0001f;

    private static readonly ClipRegion EmptyRegion =
        new(Array.Empty<Vector2>());

    private readonly Vector2[] _vertices;

    private ClipRegion(Vector2[] vertices)
    {
        _vertices = vertices;
    }

    public bool IsEmpty =>
        _vertices.Length < 3;

    public ReadOnlySpan<Vector2> Vertices =>
        _vertices;

    public Rectangle Bounds
    {
        get
        {
            if (IsEmpty)
            {
                return new Rectangle(
                    0.0f,
                    0.0f,
                    0.0f,
                    0.0f);
            }

            var minX = _vertices[0].X;
            var minY = _vertices[0].Y;
            var maxX = minX;
            var maxY = minY;

            foreach (var vertex in _vertices)
            {
                minX = MathF.Min(minX, vertex.X);
                minY = MathF.Min(minY, vertex.Y);
                maxX = MathF.Max(maxX, vertex.X);
                maxY = MathF.Max(maxY, vertex.Y);
            }

            return new Rectangle(
                minX,
                minY,
                maxX - minX,
                maxY - minY);
        }
    }

    public static ClipRegion FromRectangle(
        Rectangle rectangle)
    {
        if (!float.IsFinite(rectangle.X) ||
            !float.IsFinite(rectangle.Y) ||
            !float.IsFinite(rectangle.Width) ||
            !float.IsFinite(rectangle.Height))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rectangle),
                "Clip rectangle must be finite.");
        }

        if (rectangle.Width <= 0.0f ||
            rectangle.Height <= 0.0f)
        {
            return EmptyRegion;
        }

        return new ClipRegion(
        [
            new Vector2(
                rectangle.X,
                rectangle.Y),

            new Vector2(
                rectangle.X + rectangle.Width,
                rectangle.Y),

            new Vector2(
                rectangle.X + rectangle.Width,
                rectangle.Y + rectangle.Height),

            new Vector2(
                rectangle.X,
                rectangle.Y + rectangle.Height)
        ]);
    }

    public static ClipRegion FromVertices(
        IEnumerable<Vector2> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        var points = vertices.ToArray();

        if (points.Length < 3)
        {
            return EmptyRegion;
        }

        foreach (var point in points)
        {
            if (!float.IsFinite(point.X) ||
                !float.IsFinite(point.Y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vertices),
                    "Clip vertices must be finite.");
            }
        }

        if (MathF.Abs(SignedArea(points)) < Epsilon)
        {
            return EmptyRegion;
        }

        return new ClipRegion(points);
    }

    /// <summary>
    /// Returns the intersection of two convex clip regions.
    /// </summary>
    public ClipRegion Intersect(
        ClipRegion other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (IsEmpty || other.IsEmpty)
        {
            return EmptyRegion;
        }

        var orientation =
            MathF.Sign(SignedArea(other._vertices));

        if (orientation == 0.0f)
        {
            return EmptyRegion;
        }

        var output =
            _vertices.ToList();

        for (var edgeIndex = 0;
             edgeIndex < other._vertices.Length;
             edgeIndex++)
        {
            if (output.Count == 0)
            {
                return EmptyRegion;
            }

            var edgeStart =
                other._vertices[edgeIndex];

            var edgeEnd =
                other._vertices[
                    (edgeIndex + 1) %
                    other._vertices.Length];

            var input = output;
            output = new List<Vector2>();

            var previous =
                input[^1];

            var previousDistance =
                SignedDistance(
                    previous,
                    edgeStart,
                    edgeEnd,
                    orientation);

            var previousInside =
                previousDistance >= -Epsilon;

            foreach (var current in input)
            {
                var currentDistance =
                    SignedDistance(
                        current,
                        edgeStart,
                        edgeEnd,
                        orientation);

                var currentInside =
                    currentDistance >= -Epsilon;

                if (currentInside != previousInside)
                {
                    var denominator =
                        previousDistance -
                        currentDistance;

                    if (MathF.Abs(denominator) > Epsilon)
                    {
                        var t = Math.Clamp(
                            previousDistance / denominator,
                            0.0f,
                            1.0f);

                        output.Add(
                            new Vector2(
                                previous.X +
                                (current.X - previous.X) * t,

                                previous.Y +
                                (current.Y - previous.Y) * t));
                    }
                }

                if (currentInside)
                {
                    output.Add(current);
                }

                previous = current;
                previousDistance = currentDistance;
                previousInside = currentInside;
            }

            RemoveAdjacentDuplicates(output);
        }

        if (output.Count < 3 ||
            MathF.Abs(
                SignedArea(output.ToArray())) < Epsilon)
        {
            return EmptyRegion;
        }

        return new ClipRegion(
            output.ToArray());
    }

    private static float SignedDistance(
        Vector2 point,
        Vector2 edgeStart,
        Vector2 edgeEnd,
        float orientation)
    {
        var edgeX =
            edgeEnd.X - edgeStart.X;

        var edgeY =
            edgeEnd.Y - edgeStart.Y;

        var pointX =
            point.X - edgeStart.X;

        var pointY =
            point.Y - edgeStart.Y;

        return orientation *
            (edgeX * pointY - edgeY * pointX);
    }

    private static float SignedArea(
        Vector2[] vertices)
    {
        double area = 0.0;

        for (var i = 0; i < vertices.Length; i++)
        {
            var current = vertices[i];

            var next =
                vertices[(i + 1) % vertices.Length];

            area +=
                (double)current.X * next.Y -
                (double)next.X * current.Y;
        }

        return (float)(area * 0.5);
    }

    private static void RemoveAdjacentDuplicates(
        List<Vector2> vertices)
    {
        for (var i = vertices.Count - 1; i > 0; i--)
        {
            if (NearlyEqual(
                    vertices[i],
                    vertices[i - 1]))
            {
                vertices.RemoveAt(i);
            }
        }

        if (vertices.Count > 1 &&
            NearlyEqual(
                vertices[0],
                vertices[^1]))
        {
            vertices.RemoveAt(
                vertices.Count - 1);
        }
    }

    private static bool NearlyEqual(
        Vector2 first,
        Vector2 second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;

        return dx * dx + dy * dy <=
               Epsilon * Epsilon;
    }
}