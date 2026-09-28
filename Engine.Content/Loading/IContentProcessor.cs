using Engine.Core.Assets;

namespace Engine.Content.Loading;

public interface IContentProcessor<TSource, TResult>
    where TSource : class
    where TResult : class
{
    TResult Process(
        AssetPath path,
        TSource source,
        IContentLoadContext context);
}