using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics;

public sealed class TextureAtlasBuilderTests
{
    [Fact]
    public void Build_ReservesPaddingBetweenSprites()
    {
        var sprites = new[]
        {
        CreateSprite("first", 2, 2, 10),
        CreateSprite("second", 2, 2, 20)
    };

        var result = new TextureAtlasBuilder().Build(
            sprites,
            new TextureAtlasBuilderOptions
            {
                MaxWidth = 16,
                MaxHeight = 8,
                Padding = 2
            });

        var first = result.GetRegion("first");
        var second = result.GetRegion("second");

        var firstRight =
            (first.UV.X + first.UV.Width) * result.Texture.Width;

        var secondLeft =
            second.UV.X * result.Texture.Width;

        Assert.True(secondLeft - firstRight >= 2);
    }

    [Fact]
    public void Build_ExtrudesEdgePixels()
    {
        var pixels = new byte[]
        {
        255, 0, 0, 255
        };

        var sprite = new TextureAtlasSprite(
            "pixel",
            new TextureData(
                1,
                1,
                TextureFormat.Rgba8,
                pixels));

        var result = new TextureAtlasBuilder().Build(
            new[] { sprite },
            new TextureAtlasBuilderOptions
            {
                MaxWidth = 8,
                MaxHeight = 8,
                Padding = 2,
                Extrusion = 1
            });

        var region = result.GetRegion("pixel");

        var x = (int)(region.UV.X * result.Texture.Width);
        var y = (int)(region.UV.Y * result.Texture.Height);

        var atlasPixels = result.Texture.Pixels.Span;

        var extrudedX = x - 1;
        var extrudedY = y - 1;

        var offset =
            (extrudedY * result.Texture.Width + extrudedX) * 4;

        Assert.Equal((byte)255, atlasPixels[offset]);
        Assert.Equal((byte)0, atlasPixels[offset + 1]);
        Assert.Equal((byte)0, atlasPixels[offset + 2]);
        Assert.Equal((byte)255, atlasPixels[offset + 3]);
    }

    [Fact]
    public void Build_RejectsExtrusionGreaterThanPadding()
    {
        var sprites = new[]
        {
        CreateSprite("first", 2, 2, 10)
    };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TextureAtlasBuilder().Build(
                sprites,
                new TextureAtlasBuilderOptions
                {
                    Padding = 1,
                    Extrusion = 2
                }));
    }

    [Fact]
    public void Build_PacksSpritesIntoAtlas()
    {
        var sprites = new[]
        {
            CreateSprite("small", 2, 2, 10),
            CreateSprite("large", 4, 4, 20)
        };

        var result = new TextureAtlasBuilder().Build(
            sprites,
            new TextureAtlasBuilderOptions
            {
                MaxWidth = 8,
                MaxHeight = 8
            });

        Assert.Equal(6, result.Texture.Width);
        Assert.Equal(4, result.Texture.Height);

        Assert.Equal(2, result.Regions.Count);

        Assert.True(result.Regions.ContainsKey("small"));
        Assert.True(result.Regions.ContainsKey("large"));
    }

    [Fact]
    public void Build_PreservesSpritePixels()
    {
        var sprites = new[]
        {
            CreateSprite("first", 2, 2, 10),
            CreateSprite("second", 2, 2, 20)
        };

        var result = new TextureAtlasBuilder().Build(
            sprites,
            new TextureAtlasBuilderOptions
            {
                MaxWidth = 8,
                MaxHeight = 8
            });

        var pixels = result.Texture.Pixels.Span;

        var first = result.GetRegion("first");
        var second = result.GetRegion("second");

        var firstX = (int)(
            first.UV.X * result.Texture.Width);

        var firstY = (int)(
            first.UV.Y * result.Texture.Height);

        var secondX = (int)(
            second.UV.X * result.Texture.Width);

        var secondY = (int)(
            second.UV.Y * result.Texture.Height);

        Assert.Equal(
            (byte)10,
            pixels[(firstY * result.Texture.Width + firstX) * 4]);

        Assert.Equal(
            (byte)20,
            pixels[(secondY * result.Texture.Width + secondX) * 4]);
    }

    [Fact]
    public void Build_UsesStableOrderForEqualSprites()
    {
        var sprites = new[]
        {
            CreateSprite("first", 2, 2, 10),
            CreateSprite("second", 2, 2, 20)
        };

        var result = new TextureAtlasBuilder().Build(
            sprites,
            new TextureAtlasBuilderOptions
            {
                MaxWidth = 8,
                MaxHeight = 8
            });

        Assert.True(
            result.GetRegion("first").UV.X <
            result.GetRegion("second").UV.X);
    }

    [Fact]
    public void Build_RejectsDuplicateNames()
    {
        var sprites = new[]
        {
            CreateSprite("ship", 2, 2, 10),
            CreateSprite("ship", 4, 4, 20)
        };

        Assert.Throws<ArgumentException>(
            () => new TextureAtlasBuilder().Build(sprites));
    }

    [Fact]
    public void Build_RejectsSpritesThatDoNotFit()
    {
        var sprites = new[]
        {
            CreateSprite("first", 8, 8, 10),
            CreateSprite("second", 8, 8, 20)
        };

        Assert.Throws<InvalidOperationException>(
            () => new TextureAtlasBuilder().Build(
                sprites,
                new TextureAtlasBuilderOptions
                {
                    MaxWidth = 8,
                    MaxHeight = 8
                }));
    }

    [Fact]
    public void Build_CreatesAtlasWithNamedRegions()
    {
        var sprites = new[]
        {
            CreateSprite("ship", 4, 4, 10),
            CreateSprite("planet", 2, 2, 20)
        };

        var result = new TextureAtlasBuilder().Build(
            sprites);

        var atlas = result.CreateAtlas(
            new TextureHandle(1));

        Assert.Equal(2, atlas.Count);

        Assert.Equal(
            result.GetRegion("ship"),
            atlas.GetRegion(0));

        Assert.Equal(
            result.GetRegion("planet"),
            atlas.GetRegion(1));
    }

    private static TextureAtlasSprite CreateSprite(
        string name,
        int width,
        int height,
        byte value)
    {
        var pixels = new byte[width * height * 4];

        Array.Fill(pixels, value);

        return new TextureAtlasSprite(
            name,
            new TextureData(
                width,
                height,
                TextureFormat.Rgba8,
                pixels));
    }
}