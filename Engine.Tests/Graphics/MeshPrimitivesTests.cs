
using Engine.Core.Math;
using Engine.Graphics3D;
using Xunit;

namespace Engine.Tests.Graphics;

public sealed class MeshPrimitivesTests
{
    [Fact]
    public void CreateTriangle_ReturnsValidTriangle()
    {
        var mesh = MeshPrimitives.CreateTriangle();

        Assert.Equal(3, mesh.VertexCount);
        Assert.Equal(3, mesh.IndexCount);
        Assert.Equal(1, mesh.TriangleCount);

        Assert.All(
            mesh.Vertices.ToArray(),
            vertex => Assert.Equal(Vector3.UnitZ, vertex.Normal));
    }

    [Fact]
    public void CreateTriangle_HasCounterClockwiseWinding()
    {
        var mesh = MeshPrimitives.CreateTriangle();

        var a = mesh.Vertices.Span[(int)mesh.Indices.Span[0]].Position;
        var b = mesh.Vertices.Span[(int)mesh.Indices.Span[1]].Position;
        var c = mesh.Vertices.Span[(int)mesh.Indices.Span[2]].Position;

        var normal = Vector3.Cross(b - a, c - a);

        Assert.True(Vector3.Dot(normal, Vector3.UnitZ) > 0);
    }


    [Fact]
    public void CreateTetrahedron_ReturnsFourFaces()
    {
        var mesh = MeshPrimitives.CreateTetrahedron();

        Assert.Equal(12, mesh.VertexCount);
        Assert.Equal(12, mesh.IndexCount);
        Assert.Equal(4, mesh.TriangleCount);
    }

    [Fact]
    public void CreateTetrahedron_HasOutwardFacingNormals()
    {
        var mesh = MeshPrimitives.CreateTetrahedron();

        var vertices = mesh.Vertices.Span;
        var indices = mesh.Indices.Span;

        for (var i = 0; i < indices.Length; i += 3)
        {
            var a = vertices[(int)indices[i]];
            var b = vertices[(int)indices[i + 1]];
            var c = vertices[(int)indices[i + 2]];

            var geometricNormal = Vector3.Cross(
                b.Position - a.Position,
                c.Position - a.Position);

            Assert.True(
                Vector3.Dot(geometricNormal, a.Normal) > 0);
        }
    }

    [Fact]
    public void CreateTetrahedron_HasFourDistinctNormals()
    {
        var mesh = MeshPrimitives.CreateTetrahedron();

        var normals = mesh.Vertices
            .ToArray()
            .Select(vertex => vertex.Normal)
            .Distinct()
            .ToArray();

        Assert.Equal(4, normals.Length);
    }

    [Fact]
    public void CreateTetrahedron_HasRequestedEdgeLength()
    {
        const float size = 3.0f;

        var mesh = MeshPrimitives.CreateTetrahedron(size);

        var first = mesh.Vertices.Span[0].Position;
        var second = mesh.Vertices.Span[1].Position;

        Assert.Equal(size, Vector3.Distance(first, second), 5);
    }


    [Fact]
    public void CreateTriangle_UsesRequestedSize()
    {
        var mesh = MeshPrimitives.CreateTriangle(4.0f);

        Assert.Equal(
            new Vector3(-2, -2, 0),
            mesh.Vertices.Span[0].Position);

        Assert.Equal(
            new Vector3(2, -2, 0),
            mesh.Vertices.Span[1].Position);
    }

    [Fact]
    public void CreateCube_ReturnsSixFaces()
    {
        var mesh = MeshPrimitives.CreateCube();

        Assert.Equal(24, mesh.VertexCount);
        Assert.Equal(36, mesh.IndexCount);
        Assert.Equal(12, mesh.TriangleCount);
    }

    [Fact]
    public void CreateCube_HasAllSixFaceNormals()
    {
        var mesh = MeshPrimitives.CreateCube();

        var normals = mesh.Vertices
            .ToArray()
            .Select(vertex => vertex.Normal)
            .Distinct()
            .ToArray();

        Assert.Equal(6, normals.Length);

        Assert.Contains(Vector3.UnitX, normals);
        Assert.Contains(-Vector3.UnitX, normals);

        Assert.Contains(Vector3.UnitY, normals);
        Assert.Contains(-Vector3.UnitY, normals);

        Assert.Contains(Vector3.UnitZ, normals);
        Assert.Contains(-Vector3.UnitZ, normals);
    }

    [Fact]
    public void CreateCube_AllTrianglesHaveOutwardWinding()
    {
        var mesh = MeshPrimitives.CreateCube();

        var vertices = mesh.Vertices.Span;
        var indices = mesh.Indices.Span;

        for (var i = 0; i < indices.Length; i += 3)
        {
            var a = vertices[(int)indices[i]];
            var b = vertices[(int)indices[i + 1]];
            var c = vertices[(int)indices[i + 2]];

            var geometricNormal = Vector3.Cross(
                b.Position - a.Position,
                c.Position - a.Position);

            Assert.True(
                Vector3.Dot(geometricNormal, a.Normal) > 0,
                $"Triangle at index {i} has incorrect winding.");
        }
    }

    [Fact]
    public void CreateCube_AllIndicesAreInsideVertexRange()
    {
        var mesh = MeshPrimitives.CreateCube();

        foreach (var index in mesh.Indices.Span)
        {
            Assert.True(index < mesh.VertexCount);
        }
    }

    [Fact]
    public void CreateCube_UsesNormalizedFaceNormals()
    {
        var mesh = MeshPrimitives.CreateCube();

        foreach (var vertex in mesh.Vertices.Span)
        {
            Assert.Equal(1.0f, vertex.Normal.Length(), 5);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void CreateTriangle_RejectsInvalidSize(float size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MeshPrimitives.CreateTriangle(size));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void CreateCube_RejectsInvalidSize(float size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MeshPrimitives.CreateCube(size));
    }
}
