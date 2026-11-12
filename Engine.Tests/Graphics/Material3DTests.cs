
using Engine.Core.Math;
using Xunit;

namespace Engine.Tests.Graphics;

public sealed class Material3DTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var material = Material3D.Default;

        Assert.Equal(
            new MaterialColor(0.25f, 0.65f, 1.0f),
            material.BaseColor);

        Assert.Equal(0.2f, material.AmbientStrength);
        Assert.Equal(0.8f, material.DiffuseStrength);
    }

    [Fact]
    public void Constructor_StoresProvidedValues()
    {
        var color = new MaterialColor(1f, 0.2f, 0.1f);

        var material = new Material3D(
            color,
            ambientStrength: 0.3f,
            diffuseStrength: 0.7f);

        Assert.Equal(color, material.BaseColor);
        Assert.Equal(0.3f, material.AmbientStrength);
        Assert.Equal(0.7f, material.DiffuseStrength);
    }

    [Fact]
    public void Constructor_RejectsInvalidColor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Material3D(new MaterialColor(1.5f, 0f, 0f)));
    }

    [Fact]
    public void Constructor_RejectsInvalidAmbientStrength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Material3D(
                new MaterialColor(),
                ambientStrength: -0.1f));
    }

    [Fact]
    public void Constructor_RejectsInvalidDiffuseStrength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Material3D(
                new MaterialColor(),
                diffuseStrength: 1.1f));
    }
}
