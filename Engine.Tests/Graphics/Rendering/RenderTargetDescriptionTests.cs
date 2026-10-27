using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderTargetDescriptionTests
{
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