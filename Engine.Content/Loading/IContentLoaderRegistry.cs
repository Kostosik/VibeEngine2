namespace Engine.Content.Loading;

public interface IContentLoaderRegistry
{
    void Register<T>(
        IContentLoader<T> loader)
        where T : class;

    bool TryGet<T>(
        out IContentLoader<T>? loader)
        where T : class;

    void Clear();
}