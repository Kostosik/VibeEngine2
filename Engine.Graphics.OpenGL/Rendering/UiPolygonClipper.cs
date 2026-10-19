
using Engine.Core.Math;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.OpenGL.Rendering;

internal readonly record struct UiClipVertex(
    Vector2 Position,
    Vector2 UV);

internal static class UiPolygonClipper
{
    private const float Epsilon = 0.0001f;

    public static ClipRegion Combine(
        ClipRegion region,
        Rectangle? clipRect)
    {
        return clipRect.HasValue
            ? region.Intersect(
                ClipRegion.FromRectangle(clipRect.Value))
            : region;
    }

    public static Vector2 RotatePoint(
        Vector2 point,
        Vector2 pivot,
        float degrees)
    {
        if (MathF.Abs(degrees) < Epsilon)
        {
            return point;
        }

        var radians = degrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var x = point.X - pivot.X;
        var y = point.Y - pivot.Y;

        return new Vector2(
            pivot.X + x * cos - y * sin,
            pivot.Y + x * sin + y * cos);
    }

    public static List<UiClipVertex> ClipPolygon(
        IReadOnlyList<UiClipVertex> polygon,
        ClipRegion region)
    {
        if (polygon.Count < 3 || region.IsEmpty)
        {
            return new List<UiClipVertex>();
        }

        var clipVertices = region.Vertices.ToArray();
        var orientation = MathF.Sign(SignedArea(clipVertices));

        if (orientation == 0.0f)
        {
            return new List<UiClipVertex>();
        }

        var output = polygon.ToList();

        for (var edgeIndex = 0;
             edgeIndex < clipVertices.Length;
             edgeIndex++)
        {
            if (output.Count == 0)
            {
                return output;
            }

            var edgeStart = clipVertices[edgeIndex];
            var edgeEnd =
                clipVertices[(edgeIndex + 1) % clipVertices.Length];

            var input = output;
            output = new List<UiClipVertex>();

            var previous = input[^1];
            var previousDistance = SignedDistance(
                previous.Position,
                edgeStart,
                edgeEnd,
                orientation);

            var previousInside =
                previousDistance >= -Epsilon;

            foreach (var current in input)
            {
                var currentDistance = SignedDistance(
                    current.Position,
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

                        output.Add(
                            Interpolate(previous, current, t));
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

        return output;
    }

    public static List<Vector2> ClipPositions(
        IReadOnlyList<Vector2> polygon,
        ClipRegion region)
    {
        var input = polygon
            .Select(
                static point => new UiClipVertex(
                    point,
                    Vector2.Zero))
            .ToArray();

        return ClipPolygon(input, region)
            .Select(static vertex => vertex.Position)
            .ToList();
    }

    public static bool ClipSegment(
        Vector2 start,
        Vector2 end,
        ClipRegion region,
        out Vector2 clippedStart,
        out Vector2 clippedEnd)
    {
        clippedStart = start;
        clippedEnd = end;

        if (region.IsEmpty)
        {
            return false;
        }

        var clipVertices = region.Vertices.ToArray();
        var orientation = MathF.Sign(SignedArea(clipVertices));

        if (orientation == 0.0f)
        {
            return false;
        }

        var firstT = 0.0f;
        var lastT = 1.0f;

        foreach (var pointIndex in Enumerable.Range(
                     0,
                     clipVertices.Length))
        {
            var edgeStart = clipVertices[pointIndex];
            var edgeEnd =
                clipVertices[(pointIndex + 1) % clipVertices.Length];

            var startDistance = SignedDistance(
                start,
                edgeStart,
                edgeEnd,
                orientation) + Epsilon;

            var endDistance = SignedDistance(
                end,
                edgeStart,
                edgeEnd,
                orientation) + Epsilon;

            var startInside = startDistance >= 0.0f;
            var endInside = endDistance >= 0.0f;

            if (!startInside && !endInside)
            {
                return false;
            }

            if (startInside == endInside)
            {
                continue;
            }

            var denominator = startDistance - endDistance;

            if (MathF.Abs(denominator) <= Epsilon)
            {
                return false;
            }

            var t = Math.Clamp(
                startDistance / denominator,
                0.0f,
                1.0f);

            if (!startInside)
            {
                firstT = MathF.Max(firstT, t);
            }
            else
            {
                lastT = MathF.Min(lastT, t);
            }

            if (firstT > lastT)
            {
                return false;
            }
        }

        clippedStart = Interpolate(start, end, firstT);
        clippedEnd = Interpolate(start, end, lastT);

        return true;
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
        IReadOnlyList<Vector2> vertices)
    {
        double area = 0.0;

        for (var i = 0; i < vertices.Count; i++)
        {
            var current = vertices[i];
            var next = vertices[(i + 1) % vertices.Count];

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

        return dx * dx + dy * dy <= Epsilon * Epsilon;
    }
}
