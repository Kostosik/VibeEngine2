using Engine.Audio.Data;

namespace Engine.Audio;

public interface IAudioDevice : IDisposable
{
    IAudioBuffer CreateBuffer(AudioData data);

    IAudioSource CreateSource(IAudioBuffer buffer);

    IAudioListener CreateListener();
}