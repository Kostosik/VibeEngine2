using Engine.Core.Math;

namespace Engine.Audio;

public interface IAudioListener : IDisposable
{
    void SetPosition(Vector3 position);
    void SetVelocity(Vector3 velocity);
    void SetGain(float gain);
    void SetOrientation(Vector3 forward, Vector3 up);
}