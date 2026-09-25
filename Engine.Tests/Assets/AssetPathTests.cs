using Engine.Core.Assets;

namespace Engine.Tests.Assets;

public sealed class AssetPathTests
{
    [Fact]
    public void PathNormalizesDirectorySeparators()
    {
        var path =
            new AssetPath(
                "Textures\\Tiles\\Grass.png");

        Assert.Equal(
            "Textures/Tiles/Grass.png",
            path.Value);
    }
}