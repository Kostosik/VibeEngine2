
using Xunit;
using Engine.Graphics3D.Resources;
// Добавь using для namespace, в котором находятся Material3D
// и MaterialColor, по примеру остальных тестов проекта.

public class Material3DTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var material = Material3D.Default;

        Assert.Equal(
            new Material3D(
            material.BaseColor);

        Assert.Equal(0.2f, material.AmbientStrength);
        Assert.Equal(0.8f, material.DiffuseStrength);
    }

    [Fact]
    public void Constructor_StoresProvidedValues()
    {
        var color = new MaterialColor(1f, 0.2f, 0.1f, 1f);

        var material = new Material3D(color, 0.3f, 0.7f);

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
    public void Constructor_RejectsInvalidLightingStrength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Material3D(
                MaterialColor.White,
                ambientStrength: -0.1f));
    }
}
