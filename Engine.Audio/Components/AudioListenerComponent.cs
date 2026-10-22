namespace Engine.Audio.Components;

public struct AudioListenerComponent
{
    public AudioListenerComponent()
    {
        Gain = 1.0f;
        Enabled = true;
    }

    public AudioListenerComponent(
        float gain,
        bool enabled = true)
    {
        Gain = gain;
        Enabled = enabled;
    }

    public float Gain { get; set; }

    public bool Enabled { get; set; }
}