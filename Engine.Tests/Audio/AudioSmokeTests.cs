using System.Buffers.Binary;
using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Core.Assets;

namespace Engine.Tests.Audio;

public sealed class AudioSmokeTests
{
    [Fact]
    public void LoadWavAndPlayThroughOpenAL()
    {
        var path =
            new AssetPath(
                "Sounds/smoke.wav");

        var wav =
            CreateTestWav();

        var assets =
            new InMemoryAssetSource(
                path,
                wav);

        using var device =
            new OpenALAudioDevice();

        using var audio =
            new AudioManager(
                assets,
                device);

        var buffer =
            audio.Load(
                path);

        Assert.NotNull(
            buffer);

        using var source =
            audio.CreateSource(
                buffer);

        source.SetLooping(
            true);

        source.Play();

        Thread.Sleep(
            100);

        source.Stop();

        Thread.Sleep(
            100);

        source.Stop();
    }

    private static byte[] CreateTestWav()
    {
        const int sampleRate = 44100;
        const ushort channels = 1;
        const ushort bitsPerSample = 16;
        const double frequency = 440.0;
        const double durationSeconds = 0.25;

        var sampleCount =
            (int)(
                sampleRate *
                durationSeconds);

        var bytesPerSample =
            bitsPerSample /
            8;

        var dataSize =
            sampleCount *
            channels *
            bytesPerSample;

        var fileSize =
            44 +
            dataSize;

        var data =
            new byte[fileSize];

        data[0] = (byte)'R';
        data[1] = (byte)'I';
        data[2] = (byte)'F';
        data[3] = (byte)'F';

        BinaryPrimitives.WriteUInt32LittleEndian(
            data.AsSpan(4),
            (uint)(
                fileSize -
                8));

        data[8] = (byte)'W';
        data[9] = (byte)'A';
        data[10] = (byte)'V';
        data[11] = (byte)'E';

        data[12] = (byte)'f';
        data[13] = (byte)'m';
        data[14] = (byte)'t';
        data[15] = (byte)' ';

        BinaryPrimitives.WriteUInt32LittleEndian(
            data.AsSpan(16),
            16);

        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(20),
            1);

        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(22),
            channels);

        BinaryPrimitives.WriteUInt32LittleEndian(
            data.AsSpan(24),
            sampleRate);

        BinaryPrimitives.WriteUInt32LittleEndian(
            data.AsSpan(28),
            (uint)(
                sampleRate *
                channels *
                bytesPerSample));

        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(32),
            (ushort)(
                channels *
                bytesPerSample));

        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(34),
            bitsPerSample);

        data[36] = (byte)'d';
        data[37] = (byte)'a';
        data[38] = (byte)'t';
        data[39] = (byte)'a';

        BinaryPrimitives.WriteUInt32LittleEndian(
            data.AsSpan(40),
            (uint)dataSize);

        var amplitude =
            short.MaxValue * 0.2;

        var sampleOffset =
            44;

        for (var i = 0;
             i < sampleCount;
             i++)
        {
            var sample =
                (short)(
                    System.Math.Sin(
                        2.0 *
                        System.Math.PI *
                        frequency *
                        i /
                        sampleRate) *
                    amplitude);

            BinaryPrimitives.WriteInt16LittleEndian(
                data.AsSpan(
                    sampleOffset,
                    2),
                sample);

            sampleOffset += 2;
        }

        return data;
    }

    private sealed class InMemoryAssetSource :
        IAssetSource
    {
        private readonly AssetPath _path;
        private readonly byte[] _data;

        public InMemoryAssetSource(
            AssetPath path,
            byte[] data)
        {
            _path = path;
            _data = data;
        }

        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            if (path != _path)
            {
                throw new FileNotFoundException(
                    $"Asset '{path}' was not found.");
            }

            return _data;
        }
    }
}