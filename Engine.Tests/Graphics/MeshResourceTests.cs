
using Engine.Graphics.Resources;
using Engine.Graphics3D;
using Engine.Graphics3D.Resources;
using Xunit;

namespace Engine.Tests.Graphics;

public sealed class MeshResourceTests
{
    [Fact]
    public void MeshHandle_DefaultValue_IsInvalid()
    {
        var handle = default(MeshHandle);

        Assert.False(handle.IsValid);
        Assert.Equal(MeshHandle.Invalid, handle);
    }

    [Fact]
    public void MeshHandle_NonZeroValue_IsValid()
    {
        var handle = new MeshHandle(1);

        Assert.True(handle.IsValid);
    }

    [Fact]
    public void MeshDescription_StoresMeshInformation()
    {
        var description = new MeshDescription(
            vertexCount: 4,
            indexCount: 6,
            layout: Vertex3D.Layout,
            usage: GraphicsBufferUsage.Static);

        Assert.Equal(4, description.VertexCount);
        Assert.Equal(6, description.IndexCount);
        Assert.Equal(2, description.TriangleCount);
        Assert.Equal(Vertex3D.Layout, description.Layout);
        Assert.Equal(
            GraphicsBufferUsage.Static,
            description.Usage);
    }

    [Fact]
    public void MeshDescription_RejectsTooFewVertices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MeshDescription(
                2,
                3,
                Vertex3D.Layout,
                GraphicsBufferUsage.Static));
    }

    [Fact]
    public void MeshDescription_RejectsTooFewIndices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MeshDescription(
                3,
                2,
                Vertex3D.Layout,
                GraphicsBufferUsage.Static));
    }

    [Fact]
    public void MeshDescription_RejectsInvalidIndexCount()
    {
        Assert.Throws<ArgumentException>(() =>
            new MeshDescription(
                4,
                4,
                Vertex3D.Layout,
                GraphicsBufferUsage.Static));
    }

    [Fact]
    public void MeshDescription_RejectsInvalidUsage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MeshDescription(
                3,
                3,
                Vertex3D.Layout,
                (GraphicsBufferUsage)999));
    }

    [Fact]
    public void MeshDescription_RejectsNullLayout()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MeshDescription(
                3,
                3,
                null!,
                GraphicsBufferUsage.Static));
    }
}
