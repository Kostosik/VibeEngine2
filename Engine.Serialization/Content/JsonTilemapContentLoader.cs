using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;

namespace Engine.Serialization.Content;

public sealed class JsonTilemapContentLoader :
    IContentLoader<TilemapDefinition>
{
    private const string FileSuffix = ".tilemap.json";

    public Type AssetType =>
        typeof(TilemapDefinition);

    public bool CanLoad(ContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        return asset.Path.Value.EndsWith(
            FileSuffix,
            StringComparison.OrdinalIgnoreCase);
    }

    public TilemapDefinition Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return JsonTilemapDefinitionSerializer.Deserialize(
            path,
            context.ReadBytes(path));
    }

    public async ValueTask<TilemapDefinition> LoadAsync(
        AssetPath path,
        IContentLoadContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        var data = await context.ReadBytesAsync(
            path,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return JsonTilemapDefinitionSerializer.Deserialize(
            path,
            data);
    }

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(path, context);
    }
}