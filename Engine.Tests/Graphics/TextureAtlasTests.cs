using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics;

public sealed class TextureAtlasTests
{
    [Fact]
    public void AtlasCalculatesGrid()
    {
        var atlas =
            new TextureAtlas(
                new TextureHandle(1),
                new TextureDescription(
                    128,
                    64,
                    TextureFormat.Rgba8),
                32,
                32);

        Assert.Equal(
            4,
            atlas.Columns);

        Assert.Equal(
            2,
            atlas.Rows);

        Assert.Equal(
            8,
            atlas.Count);
    }

    [Fact]
    public void IndexReturnsCorrectRegion()
    {
        var atlas =
            new TextureAtlas(
                new TextureHandle(1),
                new TextureDescription(
                    128,
                    64,
                    TextureFormat.Rgba8),
                32,
                32);

        var region =
            atlas.GetRegion(5);

        Assert.Equal(
            new Rectangle(
                0.25f,
                0.5f,
                0.25f,
                0.5f),
            region.UV);
    }

    [Fact]
    public void InvalidIndexThrows()
    {
        var atlas =
            new TextureAtlas(
                new TextureHandle(1),
                new TextureDescription(
                    128,
                    64,
                    TextureFormat.Rgba8),
                32,
                32);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                atlas.GetRegion(8);
            });
    }
}