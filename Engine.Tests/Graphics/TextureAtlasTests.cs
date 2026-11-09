using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics;

public sealed class TextureAtlasTests
{
    [Fact]
    public void Atlas_RejectsZeroTextureWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TextureAtlas(
                new TextureHandle(1),
                new TextureDescription(
                    0,
                    64,
                    TextureFormat.Rgba8),
                32,
                32));
    }

    [Fact]
    public void AtlasCalculatesGrid()
    {
        var atlas = new TextureAtlas(
            new TextureHandle(1),
            new TextureDescription(
                128,
                64,
                TextureFormat.Rgba8),
            32,
            32);

        Assert.True(atlas.IsGrid);

        Assert.Equal(4, atlas.Columns);
        Assert.Equal(2, atlas.Rows);
        Assert.Equal(8, atlas.Count);
    }

    [Fact]
    public void IndexReturnsCorrectRegion()
    {
        var atlas = new TextureAtlas(
            new TextureHandle(1),
            new TextureDescription(
                128,
                64,
                TextureFormat.Rgba8),
            32,
            32);

        var region = atlas.GetRegion(5);

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
        var atlas = new TextureAtlas(
            new TextureHandle(1),
            new TextureDescription(
                128,
                64,
                TextureFormat.Rgba8),
            32,
            32);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => atlas.GetRegion(8));
    }

    [Fact]
    public void AtlasFromExplicitRegions_ReturnsRegionsInOrder()
    {
        var description = new TextureDescription(
            256,
            128,
            TextureFormat.Rgba8);

        var regions = new[]
        {
            AtlasRegion.FromPixels(
                0, 0, 64, 32, description),

            AtlasRegion.FromPixels(
                64, 0, 128, 64, description),

            AtlasRegion.FromPixels(
                0, 64, 32, 48, description)
        };

        var atlas = new TextureAtlas(
            new TextureHandle(1),
            description,
            regions);

        Assert.False(atlas.IsGrid);

        Assert.Equal(3, atlas.Count);

        Assert.Equal(0, atlas.Columns);
        Assert.Equal(0, atlas.Rows);

        Assert.Equal(regions[0], atlas.GetRegion(0));
        Assert.Equal(regions[1], atlas.GetRegion(1));
        Assert.Equal(regions[2], atlas.GetRegion(2));
    }

    [Fact]
    public void AtlasFromExplicitRegions_DoesNotDependOnSourceCollection()
    {
        var description = new TextureDescription(
            128,
            128,
            TextureFormat.Rgba8);

        var regions = new List<AtlasRegion>
        {
            AtlasRegion.FromPixels(
                0, 0, 32, 32, description)
        };

        var atlas = new TextureAtlas(
            new TextureHandle(1),
            description,
            regions);

        regions.Clear();

        Assert.Equal(1, atlas.Count);

        Assert.Equal(
            new Rectangle(
                0.0f,
                0.0f,
                0.25f,
                0.25f),
            atlas.GetRegion(0).UV);
    }

    [Fact]
    public void ExplicitAtlas_RejectsGridCoordinates()
    {
        var description = new TextureDescription(
            128,
            128,
            TextureFormat.Rgba8);

        var atlas = new TextureAtlas(
            new TextureHandle(1),
            description,
            new[]
            {
                AtlasRegion.FromPixels(
                    0, 0, 32, 32, description)
            });

        Assert.Throws<InvalidOperationException>(
            () => atlas.GetRegion(0, 0));
    }

    [Fact]
    public void AtlasRegionFromPixels_CalculatesNormalizedUv()
    {
        var description = new TextureDescription(
            256,
            128,
            TextureFormat.Rgba8);

        var region = AtlasRegion.FromPixels(
            32,
            16,
            64,
            32,
            description);

        Assert.Equal(
            new Rectangle(
                0.125f,
                0.125f,
                0.25f,
                0.25f),
            region.UV);
    }

    [Fact]
    public void AtlasRegionFromPixels_RejectsOutOfBounds()
    {
        var description = new TextureDescription(
            128,
            128,
            TextureFormat.Rgba8);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => AtlasRegion.FromPixels(
                120,
                0,
                16,
                16,
                description));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => AtlasRegion.FromPixels(
                0,
                120,
                16,
                16,
                description));
    }

    [Fact]
    public void ExplicitAtlas_RejectsInvalidUv()
    {
        var description = new TextureDescription(
            128,
            128,
            TextureFormat.Rgba8);

        var invalidRegion = new AtlasRegion(
            new Rectangle(
                0.9f,
                0.0f,
                0.2f,
                0.2f));

        Assert.Throws<ArgumentException>(
            () => new TextureAtlas(
                new TextureHandle(1),
                description,
                new[] { invalidRegion }));
    }

    [Fact]
    public void ExplicitAtlas_RejectsEmptyRegions()
    {
        Assert.Throws<ArgumentException>(
            () => new TextureAtlas(
                new TextureHandle(1),
                new TextureDescription(
                    128,
                    128,
                    TextureFormat.Rgba8),
                Array.Empty<AtlasRegion>()));
    }
}