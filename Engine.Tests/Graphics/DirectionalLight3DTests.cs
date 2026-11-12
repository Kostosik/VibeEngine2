using Engine.Core.Math;
using Engine.Graphics3D.Lighting;
using Xunit;

namespace Engine.Tests.Graphics;

public sealed class DirectionalLight3DTests
{
    [Fact]
    public void Constructor_NormalizesDirection()
    {
        var light = new DirectionalLight3D(
            new Vector3(0, 5, 0),
            Vector3.One);

        Assert.Equal(Vector3.UnitY, light.DirectionToLight);
    }

    [Fact]
    public void Constructor_RejectsZeroDirection()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DirectionalLight3D(
                Vector3.Zero,
                Vector3.One));
    }

    [Fact]
    public void Default_HasExpectedValues()
    {
        var light = DirectionalLight3D.Default;

        Assert.Equal(Vector3.One, light.Color);
        Assert.Equal(1.0f, light.Intensity);
    }
}