using Engine.Core.Assets;

namespace Engine.Content.Loading;

public interface IContentLoadContext
{
    ReadOnlyMemory<byte> ReadBytes(
        AssetPath path);

    T Load<T>(
        AssetPath path)
        where T : class;
}