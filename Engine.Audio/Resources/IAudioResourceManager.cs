using Engine.Core.Assets;

namespace Engine.Audio.Resources;

public interface IAudioResourceManager : IDisposable
{
    IAudioBuffer Load(AssetPath path);

    bool IsLoaded(AssetPath path);

    bool TryGet(
        AssetPath path,
        out IAudioBuffer buffer);

    bool Unload(AssetPath path);

    void UnloadAll();
}