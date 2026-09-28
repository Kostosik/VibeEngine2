using Engine.Core.Assets;

namespace Engine.Content.Loading;

public interface IContentLoader<T>
    where T : class
{
    T Load(
        AssetPath path,
        IContentLoadContext context);
}