using Engine.Animation;

namespace Engine.Tests.Animation;

public sealed class AnimationClipFileServiceTests
{
    [Fact]
    public void SaveLoad_RoundTripsAnimationClip()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(
            directory,
            "Walk.animation.json");

        try
        {
            var clip = new AnimationClip(2.0);

            var trackId = new AnimationTrackId("opacity");

            clip.AddTrack(
                trackId,
                new AnimationTrack<float>(
                    new[]
                    {
                        new AnimationKeyframe<float>(0.0, 0.0f),
                        new AnimationKeyframe<float>(2.0, 1.0f)
                    },
                    new FloatLinearAnimationInterpolator()));

            var service = new AnimationClipFileService();

            service.Save(path, clip);

            Assert.True(File.Exists(path));

            var loaded = service.Load(path);

            Assert.Equal(2.0, loaded.DurationSeconds);
            Assert.Equal(1, loaded.TrackCount);

            Assert.Equal(
                0.5f,
                loaded.Sample<float>(trackId, 1.0),
                precision: 5);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_ThrowsWhenFileDoesNotExist()
    {
        var service = new AnimationClipFileService();

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.animation.json");

        Assert.Throws<FileNotFoundException>(
            () => service.Load(path));
    }
}