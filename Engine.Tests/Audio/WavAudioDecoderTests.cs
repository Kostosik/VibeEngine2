using System.Buffers.Binary;
using Engine.Audio.Data;
using Engine.Audio.Loading;
using Engine.Core.Assets;

namespace Engine.Tests.Audio;

public sealed class WavAudioDecoderTests
{
    [Fact]
    public void Load_AcceptsValidPcmWav()
    {
        var wav =
            CreateWav(
                blockAlign: 2,
                byteRate: 44100 * 2,
                data: new byte[] { 0, 0 });

        var loader =
            new AudioLoader();

        var result =
            loader.Load(
                new InMemoryAssetSource(wav),
                new AssetPath("test.wav"));

        Assert.Equal(
            44100,
            result.SampleRate);

        Assert.Equal(
            AudioFormat.Mono16,
            result.Format);
    }

    [Fact]
    public void Load_RejectsInvalidBlockAlign()
    {
        var wav =
            CreateWav(
                blockAlign: 1,
                byteRate: 44100 * 2,
                data: new byte[] { 0, 0 });

        var loader =
            new AudioLoader();

        Assert.Throws<InvalidDataException>(
            () =>
                loader.Load(
                    new InMemoryAssetSource(wav),
                    new AssetPath("test.wav")));
    }

    [Fact]
    public void Load_RejectsInvalidByteRate()
    {
        var wav =
            CreateWav(
                blockAlign: 2,
                byteRate: 44100,
                data: new byte[] { 0, 0 });

        var loader =
            new AudioLoader();

        Assert.Throws<InvalidDataException>(
            () =>
                loader.Load(
                    new InMemoryAssetSource(wav),
                    new AssetPath("test.wav")));
    }

    [Fact]
    public void Load_RejectsUnalignedDataChunk()
    {
        var wav =
            CreateWav(
                blockAlign: 2,
                byteRate: 44100 * 2,
                data: new byte[] { 0 });

        var loader =
            new AudioLoader();

        Assert.Throws<InvalidDataException>(
            () =>
                loader.Load(
                    new InMemoryAssetSource(wav),
                    new AssetPath("test.wav")));
    }

    private static byte[] CreateWav(
        ushort blockAlign,
        uint byteRate,
        byte[] data)
    {
        const int sampleRate = 44100;
        const ushort channels = 1;
        const ushort bitsPerSample = 16;

        var fileSize =
            44 +
            data.Length;

        var wav =
            new byte[fileSize];

        wav[0] = (byte)'R';
        wav[1] = (byte)'I';
        wav[2] = (byte)'F';
        wav[3] = (byte)'F';

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(4),
            (uint)(fileSize - 8));

        wav[8] = (byte)'W';
        wav[9] = (byte)'A';
        wav[10] = (byte)'V';
        wav[11] = (byte)'E';

        wav[12] = (byte)'f';
        wav[13] = (byte)'m';
        wav[14] = (byte)'t';
        wav[15] = (byte)' ';

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(16),
            16);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(20),
            1);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(22),
            channels);

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(24),
            sampleRate);

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(28),
            byteRate);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(32),
            blockAlign);

        BinaryPrimitives.WriteUInt16LittleEndian(
            wav.AsSpan(34),
            bitsPerSample);

        wav[36] = (byte)'d';
        wav[37] = (byte)'a';
        wav[38] = (byte)'t';
        wav[39] = (byte)'a';

        BinaryPrimitives.WriteUInt32LittleEndian(
            wav.AsSpan(40),
            (uint)data.Length);

        data.CopyTo(
            wav.AsSpan(44));

        return wav;
    }

    private sealed class InMemoryAssetSource :
        IAssetSource
    {
        private readonly byte[] _data;

        public InMemoryAssetSource(
            byte[] data)
        {
            _data = data;
        }

        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            return _data;
        }
    }
}