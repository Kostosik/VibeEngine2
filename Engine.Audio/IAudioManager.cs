using Engine.Core.Assets;

namespace Engine.Audio;

public interface IAudioManager : IDisposable
{
    IAudioBuffer Load(AssetPath path);

    IAudioSource CreateSource(IAudioBuffer buffer);

    IAudioSource Play(IAudioBuffer buffer);

    IAudioListener CreateListener();
}