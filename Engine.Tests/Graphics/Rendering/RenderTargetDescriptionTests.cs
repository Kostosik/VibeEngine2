using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderTargetDescriptionTests
{
    [Fact]
    public void Constructor_DefaultsToLinearColorSpace()
    {
        var description = new RenderTargetDescription(
            128,
            128);

        Assert.Equal(
            TextureColorSpace.Linear,
            description.ColorSpace);
    }

    [Fact]
    public void Validate_RejectsSrgbForFloatFormat()
    {
        var description = new RenderTargetDescription(
            128,
            128,
            TextureFormat.Rgba16Float)
        {
            ColorSpace = TextureColorSpace.Srgb
        };

        Assert.Throws<ArgumentException>(
            description.Validate);
    }

    [Fact]
    public void Validate_RejectsUnsupportedFormat()
    {
        var description = new RenderTargetDescription(
            64,
            64,
            (TextureFormat)999);

        Assert.Throws<ArgumentOutOfRangeException>(
            description.Validate);
    }
}