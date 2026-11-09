using Engine.Core.Math;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;

namespace Engine.Tests.Graphics2D;

public sealed class TextureAtlasAnimationBuilderTests
{
    [Fact]
    public void Build_CreatesAnimationFromNamedRegions()
    {
        var atlasResult = CreateAtlas();

        var animation = new TextureAtlasAnimationBuilder().Build(
            atlasResult,
            new TextureHandle(1),
            new[]
            {
                "frame_0",
                "frame_1"
            },
            new Vector2(32, 32),
            0.1f);

        Assert.Equal(2, animation.FrameCount);

        Assert.Equal(
            atlasResult.GetRegion("frame_0").UV,
            animation.Current.UV);
    }

    [Fact]
    public void Build_UsesCorrectAtlasRegionsWhenAnimationAdvances()
    {
        var atlasResult = CreateAtlas();

        var animation = new TextureAtlasAnimationBuilder().Build(
            atlasResult,
            new TextureHandle(1),
            new[]
            {
                "frame_0",
                "frame_1"
            },
            new Vector2(32, 32),
            0.1f);

        animation.Update(0.11f);

        Assert.Equal(1, animation.CurrentFrame);

        Assert.Equal(
            atlasResult.GetRegion("frame_1").UV,
            animation.Current.UV);
    }

    [Fact]
    public void Build_RejectsEmptyFrameNames()
    {
        var atlasResult = CreateAtlas();

        Assert.Throws<ArgumentException>(
            () => new TextureAtlasAnimationBuilder().Build(
                atlasResult,
                new TextureHandle(1),
                Array.Empty<string>(),
                new Vector2(32, 32),
                0.1f));
    }

    [Fact]
    public void Build_RejectsUnknownRegion()
    {
        var atlasResult = CreateAtlas();

        Assert.Throws<KeyNotFoundException>(
            () => new TextureAtlasAnimationBuilder().Build(
                atlasResult,
                new TextureHandle(1),
                new[] { "missing_frame" },
                new Vector2(32, 32),
                0.1f));
    }

    [Fact]
    public void Build_PreservesLayer()
    {
        var atlasResult = CreateAtlas();

        var animation = new TextureAtlasAnimationBuilder().Build(
            atlasResult,
            new TextureHandle(1),
            new[] { "frame_0" },
            new Vector2(32, 32),
            0.1f,
            layer: 5);

        Assert.Equal(5, animation.Current.Layer);
    }

    private static TextureAtlasBuildResult CreateAtlas()
    {
        var sprites = new[]
        {
            CreateSprite("frame_0", 2, 2, 10),
            CreateSprite("frame_1", 2, 2, 20)
        };

        return new TextureAtlasBuilder().Build(sprites);
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