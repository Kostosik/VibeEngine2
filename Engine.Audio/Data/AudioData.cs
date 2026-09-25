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

        SampleRate = sampleRate;
        Format = format;
        Samples = samples;
    }
}