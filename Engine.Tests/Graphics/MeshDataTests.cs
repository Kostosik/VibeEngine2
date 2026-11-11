
using Engine.Core.Math;
using Engine.Graphics3D;

namespace Engine.Tests.Graphics;

public sealed class MeshDataTests
{
    private static Vertex3D[] CreateTriangleVertices()
    {
        return
        [
            new Vertex3D(
                new Vector3(0, 0, 0),
                Vector3.UnitZ,
                Vector2.Zero),

            new Vertex3D(
                new Vector3(1, 0, 0),
                Vector3.UnitZ,
                Vector2.Zero),

            new Vertex3D(
                new Vector3(0, 1, 0),
                Vector3.UnitZ,
                Vector2.Zero)
        ];
    }

    [Fact]
    public void Constructor_StoresTriangleGeometry()
    {
        var vertices = CreateTriangleVertices();

        uint[] indices = [0, 1, 2];

        var mesh = new MeshData(vertices, indices);

        Assert.Equal(3, mesh.VertexCount);
        Assert.Equal(3, mesh.IndexCount);
        Assert.Equal(1, mesh.TriangleCount);

        Assert.Equal(vertices, mesh.Vertices.ToArray());
        Assert.Equal(indices, mesh.Indices.ToArray());
    }

    [Fact]
    public void Constructor_CopiesInputArrays()
    {
        var vertices = CreateTriangleVertices();
        uint[] indices = [0, 1, 2];

        var mesh = new MeshData(vertices, indices);

        vertices[0] = new Vertex3D(
            new Vector3(99, 99, 99),
            Vector3.Zero,
            Vector2.Zero);
        indices[0] = 2;

        Assert.Equal(
            Vector3.Zero,
            mesh.Vertices.Span[0].Position);

        Assert.Equal(
            (uint)0,
            mesh.Indices.Span[0]);
    }

    [Fact]
    public void Constructor_RejectsInvalidVertexCount()
    {
        Assert.Throws<ArgumentException>(() =>
            new MeshData(
                Array.Empty<Vertex3D>(),
                new uint[] { 0, 1, 2 }));
    }

    [Fact]
    public void Constructor_RejectsInvalidIndexCount()
    {
        Assert.Throws<ArgumentException>(() =>
            new MeshData(
                CreateTriangleVertices(),
                new uint[] { 0, 1, 2, 0 }));
    }

    [Fact]
    public void Constructor_RejectsMissingVertexReference()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MeshData(
                CreateTriangleVertices(),
                new uint[] { 0, 1, 3 }));
    }

    [Fact]
    public void Constructor_RejectsNonFiniteVertex()
    {
        var vertices = CreateTriangleVertices();

        vertices[1] = new Vertex3D(
            new Vector3(float.NaN, 0, 0),
            Vector3.UnitZ,
            Vector2.Zero);

        Assert.Throws<ArgumentException>(() =>
            new MeshData(
                vertices,
                new uint[] { 0, 1, 2 }));
    }
}
