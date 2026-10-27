using Engine.Graphics2D.Sprites;

namespace Engine.Tests.Graphics2D;

public sealed class SpriteAnimationDefinitionJsonSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesFramesAndSettings()
    {
        var source = new SpriteAnimationDefinition(
            new[]
            {
                new SpriteAnimationFrameDefinition(
                    "Textures/hero_idle_0.png"),
                new SpriteAnimationFrameDefinition(
                    "Textures/hero_idle_1.png")
            },
            frameDurationSeconds: 0.08f,
            loop: false);

        var serializer = new SpriteAnimationDefinitionJsonSerializer();

        var json = serializer.Serialize(source);
        var result = serializer.Deserialize(json);

        Assert.Equal(2, result.Frames.Count);
        Assert.Equal(
            "Textures/hero_idle_0.png",
            result.Frames[0].TexturePath);
        Assert.Equal(
            "Textures/hero_idle_1.png",
            result.Frames[1].TexturePath);
        Assert.Equal(0.08f, result.FrameDurationSeconds);
        Assert.False(result.Loop);
    }

    [Fact]
    public void RoundTrip_AllowsEmptyFrameList()
    {
        var source = new SpriteAnimationDefinition();

        var serializer = new SpriteAnimationDefinitionJsonSerializer();

        var result = serializer.Deserialize(
            serializer.Serialize(source));

        Assert.Empty(result.Frames);
        Assert.Equal(0.1f, result.FrameDurationSeconds);
        Assert.True(result.Loop);
    }

    [Fact]
    public void Deserialize_RejectsUnsupportedVersion()
    {
        const string json = """
        {
          "formatVersion": 999,
          "frameDurationSeconds": 0.1,
          "loop": true,
          "frames": []
        }
        """;

        var serializer = new SpriteAnimationDefinitionJsonSerializer();

        Assert.Throws<NotSupportedException>(
            () => serializer.Deserialize(json));
    }

    [Fact]
    public void Frame_RejectsPathOutsideAssetRoot()
    {
        Assert.Throws<ArgumentException>(
            () => new SpriteAnimationFrameDefinition(
                "../outside/texture.png"));
    }
}