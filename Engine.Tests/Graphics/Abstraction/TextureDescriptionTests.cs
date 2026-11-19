using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class TextureDescriptionTests
{
    [Fact]
    public void Constructor_CreatesDescription()
    {
        var description =
            new TextureDescription(
                128,
                64,
                TextureFormat.Rgba8);

        Assert.Equal(128, description.Width);
        Assert.Equal(64, description.Height);
        Assert.Equal(
            TextureFormat.Rgba8,
            description.Format);
    }

    [Fact]
    public void Constructor_DefaultsToLinearColorSpace()
    {
        var description = new TextureDescription(
            128,
            64,
            TextureFormat.Rgba8);

        Assert.Equal(
            TextureColorSpace.Linear,
            description.ColorSpace);
    }

    [Fact]
    public void Description_CanUseSrgbColorSpace()
    {
        var description = new TextureDescription(
            128,
            64,
            TextureFormat.Rgba8)
        {
            ColorSpace = TextureColorSpace.Srgb
        };

        Assert.Equal(
            TextureColorSpace.Srgb,
            description.ColorSpace);
    }
}