using Engine.Audio.OpenAL;

namespace Engine.Tests.Audio;

public sealed class OpenALAudioOwnershipTests
{
    [Fact]
    public void CreateSource_RejectsBufferFromAnotherDevice()
    {
        using var firstDevice =
            new OpenALAudioDevice();

        using var secondDevice =
            new OpenALAudioDevice();

        var buffer =
            firstDevice.CreateBuffer(
                new Engine.Audio.Data.AudioData(
                    44100,
                    Engine.Audio.Data.AudioFormat.Mono16,
                    new byte[2]));

        Assert.Throws<ArgumentException>(
            () =>
                secondDevice.CreateSource(
                    buffer));
    }
}