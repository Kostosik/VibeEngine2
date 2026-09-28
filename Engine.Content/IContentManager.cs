using Engine.Core.Assets;

namespace Engine.Content;

public interface IContentManager
{
    T Load<T>(
        AssetPath path)
        where T : class;

    bool IsLoaded<T>(
        AssetPath path)
        where T : class;

    bool Unload<T>(
        AssetPath path)
        where T : class;

    void ClearCache();
}