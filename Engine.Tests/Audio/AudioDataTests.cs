using Engine.Audio.Data;

namespace Engine.Tests.Audio;

public sealed class AudioDataTests
{
    [Fact]
    public void Constructor_AcceptsValidSampleAlignment()
    {
        var data =
            new AudioData(
                44100,
                AudioFormat.Stereo16,
                new byte[8]);

        Assert.Equal(
            AudioFormat.Stereo16,
            data.Format);

        Assert.Equal(
            8,
            data.Samples.Length);
    }

    [Fact]
    public void Constructor_RejectsInvalidSampleAlignment()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AudioData(
                    44100,
                    AudioFormat.Stereo16,
                    new byte[6 + 1]));
    }
}