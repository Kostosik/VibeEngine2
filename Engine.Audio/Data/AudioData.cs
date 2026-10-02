namespace Engine.Audio.Data;

public sealed class AudioData
{
    public int SampleRate { get; }

    public AudioFormat Format { get; }

    public ReadOnlyMemory<byte> Samples { get; }

    public AudioData(
        int sampleRate,
        AudioFormat format,
        ReadOnlyMemory<byte> samples)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleRate),
                "Sample rate must be greater than zero.");
        }

        if (samples.IsEmpty)
        {
            throw new ArgumentException(
                "Audio samples cannot be empty.",
                nameof(samples));
        }

        var bytesPerFrame =
            GetBytesPerFrame(format);

        if (samples.Length % bytesPerFrame != 0)
        {
            throw new ArgumentException(
                $"Audio sample data length must be a multiple of " +
                $"{bytesPerFrame} bytes for format '{format}'.",
                nameof(samples));
        }

        SampleRate = sampleRate;
        Format = format;
        Samples = samples;
    }

    private static int GetBytesPerFrame(
        AudioFormat format)
    {
        return format switch
        {
            AudioFormat.Mono8 => 1,
            AudioFormat.Stereo8 => 2,
            AudioFormat.Mono16 => 2,
            AudioFormat.Stereo16 => 4,

            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported audio format.")
        };
    }
}