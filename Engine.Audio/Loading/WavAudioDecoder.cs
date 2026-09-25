using System.Buffers.Binary;
using Engine.Audio.Data;
using Engine.Core.Assets;

namespace Engine.Audio.Loading;

internal static class WavAudioDecoder
{
    public static AudioData Decode(
        ReadOnlySpan<byte> data,
        AssetPath path)
    {
        if (data.Length < 12)
        {
            throw new InvalidDataException(
                $"Audio asset '{path}' is not a valid WAV file.");
        }

        if (!data[..4].SequenceEqual("RIFF"u8) ||
            !data[8..12].SequenceEqual("WAVE"u8))
        {
            throw new InvalidDataException(
                $"Audio asset '{path}' is not a valid RIFF/WAVE file.");
        }

        var position = 12;

        ushort channels = 0;
        ushort bitsPerSample = 0;
        uint sampleRate = 0;

        ReadOnlySpan<byte> samples = default;
        bool formatFound = false;
        bool dataFound = false;

        while (position + 8 <= data.Length)
        {
            var chunkId = data.Slice(position, 4);
            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(
                data.Slice(position + 4, 4));

            position += 8;

            if (chunkSize > int.MaxValue)
            {
                throw new InvalidDataException(
                    $"WAV chunk in '{path}' is too large.");
            }

            var size = (int)chunkSize;

            if (size > data.Length - position)
            {
                throw new InvalidDataException(
                    $"WAV chunk in '{path}' extends beyond the file.");
            }

            var chunk = data.Slice(position, size);

            if (chunkId.SequenceEqual("fmt "u8))
            {
                ReadFormat(
                    chunk,
                    path,
                    out channels,
                    out sampleRate,
                    out bitsPerSample);

                formatFound = true;
            }
            else if (chunkId.SequenceEqual("data"u8))
            {
                samples = chunk;
                dataFound = true;
            }

            position += size;

            // RIFF chunks are aligned to 2 bytes.
            if ((size & 1) != 0)
            {
                if (position >= data.Length)
                    break;

                position++;
            }
        }

        if (!formatFound)
        {
            throw new InvalidDataException(
                $"WAV file '{path}' does not contain a 'fmt ' chunk.");
        }

        if (!dataFound || samples.IsEmpty)
        {
            throw new InvalidDataException(
                $"WAV file '{path}' does not contain audio data.");
        }

        var format = GetAudioFormat(
            channels,
            bitsPerSample,
            path);

        return new AudioData(
            checked((int)sampleRate),
            format,
            samples.ToArray());
    }

    private static void ReadFormat(
        ReadOnlySpan<byte> chunk,
        AssetPath path,
        out ushort channels,
        out uint sampleRate,
        out ushort bitsPerSample)
    {
        if (chunk.Length < 16)
        {
            throw new InvalidDataException(
                $"WAV file '{path}' contains an invalid 'fmt ' chunk.");
        }

        var audioFormat =
            BinaryPrimitives.ReadUInt16LittleEndian(chunk[..2]);

        channels =
            BinaryPrimitives.ReadUInt16LittleEndian(chunk.Slice(2, 2));

        sampleRate =
            BinaryPrimitives.ReadUInt32LittleEndian(chunk.Slice(4, 4));

        bitsPerSample =
            BinaryPrimitives.ReadUInt16LittleEndian(chunk.Slice(14, 2));

        if (audioFormat != 1)
        {
            throw new NotSupportedException(
                $"WAV audio format '{audioFormat}' is not supported. " +
                "Only PCM is currently supported.");
        }

        if (channels is not (1 or 2))
        {
            throw new NotSupportedException(
                $"WAV channel count '{channels}' is not supported.");
        }

        if (sampleRate == 0)
        {
            throw new InvalidDataException(
                $"WAV file '{path}' has an invalid sample rate.");
        }

        if (bitsPerSample is not (8 or 16))
        {
            throw new NotSupportedException(
                $"WAV bit depth '{bitsPerSample}' is not supported.");
        }
    }

    private static AudioFormat GetAudioFormat(
        ushort channels,
        ushort bitsPerSample,
        AssetPath path)
    {
        return (channels, bitsPerSample) switch
        {
            (1, 8) => AudioFormat.Mono8,
            (2, 8) => AudioFormat.Stereo8,
            (1, 16) => AudioFormat.Mono16,
            (2, 16) => AudioFormat.Stereo16,

            _ => throw new NotSupportedException(
                $"Unsupported WAV format in '{path}'.")
        };
    }
}