using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;

namespace Engine.Serialization.Content;

public sealed class JsonTileSetContentLoader :
    IContentLoader<TileSetDefinition>
{
    private const string FileSuffix = ".tileset.json";

    public Type AssetType =>
        typeof(TileSetDefinition);

    public bool CanLoad(ContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        return asset.Path.Value.EndsWith(
            FileSuffix,
            StringComparison.OrdinalIgnoreCase);
    }

    public TileSetDefinition Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return JsonTileSetDefinitionSerializer.Deserialize(
            path,
            context.ReadBytes(path));
    }

    public async ValueTask<TileSetDefinition> LoadAsync(
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

        return JsonTileSetDefinitionSerializer.Deserialize(
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