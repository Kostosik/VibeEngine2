using Engine.Core.Assets;

namespace Engine.Content.Loading;

public interface IContentLoadContext
{
    ReadOnlyMemory<byte> ReadBytes(
        AssetPath path);

    T Load<T>(
        AssetPath path)
        where T : class;

    ValueTask<ReadOnlyMemory<byte>> ReadBytesAsync(
    AssetPath path,
    CancellationToken cancellationToken = default);

    ValueTask<T> LoadAsync<T>(
        AssetPath path,
        CancellationToken cancellationToken = default)
        where T : class;
}