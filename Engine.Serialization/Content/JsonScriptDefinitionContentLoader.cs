using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Scripting;
using System.Text.Json;

namespace Engine.Serialization.Content;

public sealed class JsonScriptDefinitionContentLoader :
    IContentLoader<ScriptDefinition>
{
    private const string FileSuffix = ".script.json";

    public Type AssetType =>
        typeof(ScriptDefinition);

    public bool CanLoad(
        ContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(
            asset);

        return asset.Path.Value.EndsWith(
            FileSuffix,
            StringComparison.OrdinalIgnoreCase);
    }

    public ScriptDefinition Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var data =
            context.ReadBytes(path);

        return Parse(
            path,
            data);
    }

    public async ValueTask<ScriptDefinition> LoadAsync(
        AssetPath path,
        IContentLoadContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        cancellationToken.ThrowIfCancellationRequested();

        var data =
            await context.ReadBytesAsync(
                path,
                cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return Parse(
            path,
            data);
    }

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(
            path,
            context);
    }

    private static ScriptDefinition Parse(
    AssetPath path,
    ReadOnlyMemory<byte> data)
    {
        return JsonScriptDefinitionSerializer.Deserialize(
            path,
            data);
    }
}