
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
