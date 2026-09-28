namespace Engine.Core.Assets;

public interface IAsyncAssetSource
{
    ValueTask<ReadOnlyMemory<byte>> LoadAsync(
        AssetPath path,
        CancellationToken cancellationToken = default);
}