using Engine.Core.Math;

namespace Engine.Audio;

public interface IAudioSource : IDisposable
{
    void Play();
    void Pause();
    void Stop();
    void Rewind();

    void SetGain(float gain);
    void SetPitch(float pitch);
    void SetLooping(bool looping);

    void SetPosition(Vector3 position);
    void SetVelocity(Vector3 velocity);
}