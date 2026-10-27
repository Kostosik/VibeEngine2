using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class GraphicsBufferDescriptionTests
{
    [Theory]
    [InlineData((GraphicsBufferType)(-1))]
    [InlineData((GraphicsBufferType)2)]
    public void Constructor_RejectsUnsupportedType(
        GraphicsBufferType type)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new GraphicsBufferDescription(
                type,
                16,
                GraphicsBufferUsage.Static));
    }

    [Theory]
    [InlineData((GraphicsBufferUsage)(-1))]
    [InlineData((GraphicsBufferUsage)3)]
    public void Constructor_RejectsUnsupportedUsage(
        GraphicsBufferUsage usage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new GraphicsBufferDescription(
                GraphicsBufferType.Vertex,
                16,
                usage));
    }
}