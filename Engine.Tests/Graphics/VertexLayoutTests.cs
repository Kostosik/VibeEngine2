
using Engine.Graphics3D;

namespace Engine.Tests.Graphics;

public sealed class VertexLayoutTests
{
    [Fact]
    public void Vertex3D_UsesExpectedMemoryLayout()
    {
        var layout = Vertex3D.Layout;

        Assert.Equal(32, layout.StrideInBytes);
        Assert.Equal(3, layout.Attributes.Length);

        var attributes = layout.Attributes.Span;

        Assert.Equal(
            new VertexAttributeDescription(
                VertexAttributeSemantic.Position,
                VertexAttributeFormat.Float3,
                0),
            attributes[0]);

        Assert.Equal(
            new VertexAttributeDescription(
                VertexAttributeSemantic.Normal,
                VertexAttributeFormat.Float3,
                12),
            attributes[1]);

        Assert.Equal(
            new VertexAttributeDescription(
                VertexAttributeSemantic.TextureCoordinate,
                VertexAttributeFormat.Float2,
                24),
            attributes[2]);
    }

    [Fact]
    public void VertexLayout_RejectsInvalidStride()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VertexLayout(
                0,
                new[]
                {
                    new VertexAttributeDescription(
                        VertexAttributeSemantic.Position,
                        VertexAttributeFormat.Float3,
                        0)
                }));
    }

    [Fact]
    public void VertexLayout_RejectsAttributeOutsideStride()
    {
        Assert.Throws<ArgumentException>(() =>
            new VertexLayout(
                8,
                new[]
                {
                    new VertexAttributeDescription(
                        VertexAttributeSemantic.Position,
                        VertexAttributeFormat.Float3,
                        0)
                }));
    }

    [Fact]
    public void VertexLayout_RejectsDuplicateSemantics()
    {
        Assert.Throws<ArgumentException>(() =>
            new VertexLayout(
                24,
                new[]
                {
                    new VertexAttributeDescription(
                        VertexAttributeSemantic.Position,
                        VertexAttributeFormat.Float3,
                        0),

                    new VertexAttributeDescription(
                        VertexAttributeSemantic.Position,
                        VertexAttributeFormat.Float3,
                        12)
                }));
    }

    [Fact]
    public void VertexLayout_CopiesAttributeArray()
    {
        var attributes = new[]
        {
            new VertexAttributeDescription(
                VertexAttributeSemantic.Position,
                VertexAttributeFormat.Float3,
                0)
        };

        var layout = new VertexLayout(12, attributes);

        attributes[0] = new VertexAttributeDescription(
            VertexAttributeSemantic.Normal,
            VertexAttributeFormat.Float3,
            0);

        Assert.Equal(
            VertexAttributeSemantic.Position,
            layout.Attributes.Span[0].Semantic);
    }
}
