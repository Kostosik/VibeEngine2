
using Engine.Core.Math;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.OpenGL.Rendering;

/// <summary>
/// Reusable buffers for clipping convex polygons.
/// The returned list is temporary and must be consumed
/// before the next call to ClipPolygon.
/// </summary>
internal sealed class UiPolygonClipWorkspace
{
    private readonly List<UiClipVertex> _first = new(8);
    private readonly List<UiClipVertex> _second = new(8);

    public List<UiClipVertex> ClipPolygon(
        ReadOnlySpan<UiClipVertex> polygon,
        ClipRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);

        _first.Clear();
        _second.Clear();

        if (polygon.Length < 3 || region.IsEmpty)
            return _first;

        foreach (var vertex in polygon)
            _first.Add(vertex);

        var clipVertices = region.Vertices;
        var orientation = MathF.Sign(SignedArea(clipVertices));

        if (orientation == 0.0f)
            return _first;

        var current = _first;
        var next = _second;

        for (var edgeIndex = 0;
             edgeIndex < clipVertices.Length;
             edgeIndex++)
        {
            if (current.Count == 0)
                break;

            next.Clear();

            var edgeStart = clipVertices[edgeIndex];
            var edgeEnd =
                clipVertices[(edgeIndex + 1) % clipVertices.Length];

            var previous = current[^1];

            var previousDistance = SignedDistance(
                previous.Position,
                edgeStart,
                edgeEnd,
                orientation);

            var previousInside =
                previousDistance >= -Epsilon;

            for (var i = 0; i < current.Count; i++)
            {
                var vertex = current[i];

                var currentDistance = SignedDistance(
                    vertex.Position,
                    edgeStart,
                    edgeEnd,
                    orientation);

                var currentInside =
                    currentDistance >= -Epsilon;

                if (currentInside != previousInside)
                {
                    var denominator =
                        previousDistance - currentDistance;

                    if (MathF.Abs(denominator) > Epsilon)
                    {
                        var t = Math.Clamp(
                            (previousDistance + Epsilon) / denominator,
                            0.0f,
                            1.0f);

                        next.Add(
                            Interpolate(previous, vertex, t));
                    }
                }

                if (currentInside)
                    next.Add(vertex);

                previous = vertex;
                previousDistance = currentDistance;
                previousInside = currentInside;
            }

            RemoveAdjacentDuplicates(next);

            (current, next) = (next, current);
        }

        return current;
    }

    private static UiClipVertex Interpolate(
        UiClipVertex first,
        UiClipVertex second,
        float t)
    {
        return new UiClipVertex(
            Interpolate(first.Position, second.Position, t),
            Interpolate(first.UV, second.UV, t));
    }

    private static Vector2 Interpolate(
        Vector2 first,
        Vector2 second,
        float t)
    {
        return new Vector2(
            first.X + (second.X - first.X) * t,
            first.Y + (second.Y - first.Y) * t);
    }

    private static float SignedDistance(
        Vector2 point,
        Vector2 edgeStart,
        Vector2 edgeEnd,
        float orientation)
    {
        var edgeX = edgeEnd.X - edgeStart.X;
        var edgeY = edgeEnd.Y - edgeStart.Y;
        var pointX = point.X - edgeStart.X;
        var pointY = point.Y - edgeStart.Y;

        return orientation *
            (edgeX * pointY - edgeY * pointX);
    }

    private static float SignedArea(
        ReadOnlySpan<Vector2> vertices)
    {
        double area = 0.0;

        for (var i = 0; i < vertices.Length; i++)
        {
            var current = vertices[i];
            var next = vertices[(i + 1) % vertices.Length];

            area +=
                (double)current.X * next.Y -
                (double)next.X * current.Y;
        }

        return (float)(area * 0.5);
    }

    private static void RemoveAdjacentDuplicates(
        List<UiClipVertex> vertices)
    {
        for (var i = vertices.Count - 1; i > 0; i--)
        {
            if (NearlyEqual(
                    vertices[i].Position,
                    vertices[i - 1].Position))
            {
                vertices.RemoveAt(i);
            }
        }

        if (vertices.Count > 1 &&
            NearlyEqual(
                vertices[0].Position,
                vertices[^1].Position))
        {
            vertices.RemoveAt(vertices.Count - 1);
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

    private const float Epsilon = 0.0001f;
}
