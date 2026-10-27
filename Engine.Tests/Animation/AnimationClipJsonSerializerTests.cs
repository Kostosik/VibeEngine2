using Engine.Animation;
using Engine.Core.Math;

namespace Engine.Tests.Animation;

public sealed class AnimationClipJsonSerializerTests
{
    [Fact]
    public void SerializeDeserialize_PreservesAnimationData()
    {
        var serializer = new AnimationClipJsonSerializer();
        var clip = new AnimationClip(2.0);

        var floatId = new AnimationTrackId("opacity");
        var intId = new AnimationTrackId("frame");
        var vector2Id = new AnimationTrackId("position");
        var vector3Id = new AnimationTrackId("offset");

        clip.AddTrack(
            floatId,
            new AnimationTrack<float>(
                new[]
                {
                    new AnimationKeyframe<float>(0.0, 0.0f),
                    new AnimationKeyframe<float>(2.0, 1.0f)
                },
                new FloatLinearAnimationInterpolator()));

        clip.AddTrack(
            intId,
            new AnimationTrack<int>(
                new[]
                {
                    new AnimationKeyframe<int>(0.0, 0),
                    new AnimationKeyframe<int>(1.0, 1)
                },
                new StepAnimationInterpolator<int>()));

        clip.AddTrack(
            vector2Id,
            new AnimationTrack<Vector2>(
                new[]
                {
                    new AnimationKeyframe<Vector2>(
                        0.0, Vector2.Zero),

                    new AnimationKeyframe<Vector2>(
                        2.0, new Vector2(2.0f, 4.0f))
                },
                new Vector2LinearAnimationInterpolator()));

        clip.AddTrack(
            vector3Id,
            new AnimationTrack<Vector3>(
                new[]
                {
                    new AnimationKeyframe<Vector3>(
                        0.0, Vector3.Zero),

                    new AnimationKeyframe<Vector3>(
                        2.0, new Vector3(2.0f, 4.0f, 6.0f))
                },
                new Vector3LinearAnimationInterpolator()));

        var json = serializer.Serialize(clip);
        var restored = serializer.Deserialize(json);

        Assert.Equal(clip.DurationSeconds, restored.DurationSeconds);
        Assert.Equal(clip.TrackCount, restored.TrackCount);

        Assert.Equal(
            0.5f,
            restored.Sample<float>(floatId, 1.0),
            precision: 5);

        Assert.Equal(
            1,
            restored.Sample<int>(intId, 1.5));

        Assert.Equal(
            new Vector2(1.0f, 2.0f),
            restored.Sample<Vector2>(vector2Id, 1.0));

        Assert.Equal(
            new Vector3(1.0f, 2.0f, 3.0f),
            restored.Sample<Vector3>(vector3Id, 1.0));
    }

    [Fact]
    public void Deserialize_RejectsUnsupportedFormatVersion()
    {
        const string json = """
        {
          "formatVersion": 99,
          "durationSeconds": 1.0,
          "tracks": []
        }
        """;

        var serializer = new AnimationClipJsonSerializer();

        Assert.Throws<NotSupportedException>(
            () => serializer.Deserialize(json));
    }
}