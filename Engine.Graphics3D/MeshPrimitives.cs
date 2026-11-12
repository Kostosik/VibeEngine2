
using Engine.Core.Math;

namespace Engine.Graphics3D;

public static class MeshPrimitives
{
    public static MeshData CreateTriangle(float size = 1.0f)
    {
        ValidateSize(size);

        var half = size * 0.5f;

        Vertex3D[] vertices =
        [
            new(
                new Vector3(-half, -half, 0),
                Vector3.UnitZ,
                new Vector2(0, 0)),

            new(
                new Vector3(half, -half, 0),
                Vector3.UnitZ,
                new Vector2(1, 0)),

            new(
                new Vector3(0, half, 0),
                Vector3.UnitZ,
                new Vector2(0.5f, 1))
        ];

        uint[] indices = [0, 1, 2];

        return new MeshData(vertices, indices);
    }

    public static MeshData CreateCube(float size = 1.0f)
    {
        ValidateSize(size);

        var half = size * 0.5f;

        var vertices = new List<Vertex3D>(24);
        var indices = new List<uint>(36);

        // Front: +Z
        AddFace(
            vertices,
            indices,
            Vector3.UnitZ,
            new Vector3(-half, -half, half),
            new Vector3(half, -half, half),
            new Vector3(half, half, half),
            new Vector3(-half, half, half));

        // Back: -Z
        AddFace(
            vertices,
            indices,
            -Vector3.UnitZ,
            new Vector3(half, -half, -half),
            new Vector3(-half, -half, -half),
            new Vector3(-half, half, -half),
            new Vector3(half, half, -half));

        // Right: +X
        AddFace(
            vertices,
            indices,
            Vector3.UnitX,
            new Vector3(half, -half, half),
            new Vector3(half, -half, -half),
            new Vector3(half, half, -half),
            new Vector3(half, half, half));

        // Left: -X
        AddFace(
            vertices,
            indices,
            -Vector3.UnitX,
            new Vector3(-half, -half, -half),
            new Vector3(-half, -half, half),
            new Vector3(-half, half, half),
            new Vector3(-half, half, -half));

        // Top: +Y
        AddFace(
            vertices,
            indices,
            Vector3.UnitY,
            new Vector3(-half, half, half),
            new Vector3(half, half, half),
            new Vector3(half, half, -half),
            new Vector3(-half, half, -half));

        // Bottom: -Y
        AddFace(
            vertices,
            indices,
            -Vector3.UnitY,
            new Vector3(-half, -half, -half),
            new Vector3(half, -half, -half),
            new Vector3(half, -half, half),
            new Vector3(-half, -half, half));

        return new MeshData(
            vertices.ToArray(),
            indices.ToArray());
    }

    private static void AddFace(
        List<Vertex3D> vertices,
        List<uint> indices,
        Vector3 normal,
        Vector3 bottomLeft,
        Vector3 bottomRight,
        Vector3 topRight,
        Vector3 topLeft)
    {
        var first = checked((uint)vertices.Count);

        vertices.Add(new Vertex3D(
            bottomLeft,
            normal,
            new Vector2(0, 0)));

        vertices.Add(new Vertex3D(
            bottomRight,
            normal,
            new Vector2(1, 0)));

        vertices.Add(new Vertex3D(
            topRight,
            normal,
            new Vector2(1, 1)));

        vertices.Add(new Vertex3D(
            topLeft,
            normal,
            new Vector2(0, 1)));

        // Два треугольника с обходом против часовой стрелки.
        indices.Add(first);
        indices.Add(first + 1);
        indices.Add(first + 2);

        indices.Add(first);
        indices.Add(first + 2);
        indices.Add(first + 3);
    }


    public static MeshData CreateTetrahedron(float size = 1.0f)
    {
        ValidateSize(size);

        // Вершины правильного тетраэдра.
        // Длина каждого ребра равна size.
        var a = size / (2.0f * MathF.Sqrt(2.0f));

        var p0 = new Vector3(a, a, a);
        var p1 = new Vector3(a, -a, -a);
        var p2 = new Vector3(-a, a, -a);
        var p3 = new Vector3(-a, -a, a);

        var vertices = new List<Vertex3D>(12);
        var indices = new List<uint>(12);

        // Четыре треугольные грани с обходом наружу.
        AddTriangle(vertices, indices, p0, p1, p2);
        AddTriangle(vertices, indices, p0, p3, p1);
        AddTriangle(vertices, indices, p0, p2, p3);
        AddTriangle(vertices, indices, p1, p3, p2);

        return new MeshData(
            vertices.ToArray(),
            indices.ToArray());
    }


    public static MeshData CreateUvSphere(
        float radius = 1.0f,
        int longitudeSegments = 24,
        int latitudeSegments = 16)
    {
        ValidateSize(radius);

        if (longitudeSegments < 3)
            throw new ArgumentOutOfRangeException(nameof(longitudeSegments));

        if (latitudeSegments < 2)
            throw new ArgumentOutOfRangeException(nameof(latitudeSegments));

        var ringVertexCount = longitudeSegments + 1;

        var vertices = new List<Vertex3D>(
            checked(2 + (latitudeSegments - 1) * ringVertexCount));

        var indices = new List<uint>(
            checked(6 * longitudeSegments * (latitudeSegments - 1)));

        // Северный полюс.
        vertices.Add(new Vertex3D(
            new Vector3(0f, radius, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector2(0.5f, 1f)));

        // Промежуточные кольца.
        for (var latitude = 1; latitude < latitudeSegments; latitude++)
        {
            var phi = MathF.PI * latitude / latitudeSegments;
            var sinPhi = MathF.Sin(phi);
            var cosPhi = MathF.Cos(phi);

            var v = 1f - latitude / (float)latitudeSegments;

            for (var longitude = 0; longitude <= longitudeSegments; longitude++)
            {
                var u = longitude / (float)longitudeSegments;
                var theta = MathF.Tau * u;

                var normal = new Vector3(
                    sinPhi * MathF.Sin(theta),
                    cosPhi,
                    sinPhi * MathF.Cos(theta));

                var position = new Vector3(
                    normal.X * radius,
                    normal.Y * radius,
                    normal.Z * radius);

                vertices.Add(new Vertex3D(
                    position,
                    normal,
                    new Vector2(u, v)));
            }
        }

        // Южный полюс.
        var bottomIndex = (uint)vertices.Count;

        vertices.Add(new Vertex3D(
            new Vector3(0f, -radius, 0f),
            new Vector3(0f, -1f, 0f),
            new Vector2(0.5f, 0f)));

        // Верхняя часть сферы.
        const uint topIndex = 0;
        const uint firstRingStart = 1;

        for (var longitude = 0; longitude < longitudeSegments; longitude++)
        {
            indices.Add(topIndex);
            indices.Add(firstRingStart + (uint)longitude);
            indices.Add(firstRingStart + (uint)longitude + 1);
        }

        // Средняя часть: соединяем соседние кольца.
        for (var latitude = 0; latitude < latitudeSegments - 2; latitude++)
        {
            var upperStart = 1u + (uint)(latitude * ringVertexCount);
            var lowerStart = upperStart + (uint)ringVertexCount;

            for (var longitude = 0; longitude < longitudeSegments; longitude++)
            {
                var a = upperStart + (uint)longitude;
                var b = lowerStart + (uint)longitude;
                var c = upperStart + (uint)longitude + 1;
                var d = lowerStart + (uint)longitude + 1;

                indices.Add(a);
                indices.Add(b);
                indices.Add(c);

                indices.Add(c);
                indices.Add(b);
                indices.Add(d);
            }
        }

        // Нижняя часть сферы.
        var lastRingStart =
            1u + (uint)((latitudeSegments - 2) * ringVertexCount);

        for (var longitude = 0; longitude < longitudeSegments; longitude++)
        {
            indices.Add(lastRingStart + (uint)longitude);
            indices.Add(bottomIndex);
            indices.Add(lastRingStart + (uint)longitude + 1);
        }

        return new MeshData(vertices.ToArray(), indices.ToArray());
    }


    private static void AddTriangle(
        List<Vertex3D> vertices,
        List<uint> indices,
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        var normal = Vector3.Cross(
            b - a,
            c - a).Normalize();

        var first = checked((uint)vertices.Count);

        vertices.Add(new Vertex3D(
            a,
            normal,
            new Vector2(0.5f, 1.0f)));

        vertices.Add(new Vertex3D(
            b,
            normal,
            new Vector2(0.0f, 0.0f)));

        vertices.Add(new Vertex3D(
            c,
            normal,
            new Vector2(1.0f, 0.0f)));

        indices.Add(first);
        indices.Add(first + 1);
        indices.Add(first + 2);
    }


    private static void ValidateSize(float size)
    {
        if (!float.IsFinite(size) || size <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Primitive size must be finite and greater than zero.");
        }
    }
}
