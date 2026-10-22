namespace Engine.Audio.Components;

public struct AudioSourceComponent
{
    public AudioSourceComponent()
    {
        AudioPath = string.Empty;
        Gain = 1.0f;
        Pitch = 1.0f;
        Looping = false;
        PlayOnStart = true;
    }

    public AudioSourceComponent(
        string audioPath,
        float gain = 1.0f,
        float pitch = 1.0f,
        bool looping = false,
        bool playOnStart = true)
    {
        ArgumentNullException.ThrowIfNull(audioPath);

        AudioPath = audioPath;
        Gain = gain;
        Pitch = pitch;
        Looping = looping;
        PlayOnStart = playOnStart;
    }

    public string AudioPath { get; set; }

    public float Gain { get; set; }

    public float Pitch { get; set; }

    public bool Looping { get; set; }

    public bool PlayOnStart { get; set; }
}