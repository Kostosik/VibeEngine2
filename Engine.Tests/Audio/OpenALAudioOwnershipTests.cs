using Engine.Audio.OpenAL;

namespace Engine.Tests.Audio;

public sealed class OpenALAudioOwnershipTests
{
    [Fact]
    public void ChildResources_RejectHandlesAfterDeviceIsDisposed()
    {
        var device = new OpenALAudioDevice();

        var buffer = Assert.IsType<OpenALAudioBuffer>(
            device.CreateBuffer(
                new Engine.Audio.Data.AudioData(
                    44100,
                    Engine.Audio.Data.AudioFormat.Mono16,
                    new byte[2])));

        var source = Assert.IsType<OpenALAudioSource>(
            device.CreateSource(buffer));

        try
        {
            device.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => _ = buffer.Handle);

            Assert.Throws<ObjectDisposedException>(
                () => _ = source.Handle);
        }
        finally
        {
            source.Dispose();
            buffer.Dispose();
            device.Dispose();
        }
    }

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