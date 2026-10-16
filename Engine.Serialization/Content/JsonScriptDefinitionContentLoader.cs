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
        if (data.IsEmpty)
        {
            throw new InvalidDataException(
                $"Script definition '{path}' is empty.");
        }

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(data.Span);

            if (json.Length > 0 && json[0] == '\uFEFF')
            {
                json = json[1..];
            }

            using var document = JsonDocument.Parse(json);

            var root =
                document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' must be a JSON object.");
            }

            if (!root.TryGetProperty(
                    "formatVersion",
                    out var versionElement) ||
                !versionElement.TryGetInt32(
                    out var formatVersion))
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' has no valid formatVersion.");
            }

            if (formatVersion !=
                ScriptDefinition.CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' uses unsupported format version " +
                    $"'{formatVersion}'.");
            }

            if (!root.TryGetProperty(
                    "scriptId",
                    out var idElement) ||
                idElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' has no valid scriptId.");
            }

            var scriptId =
                idElement.GetString();

            if (string.IsNullOrWhiteSpace(scriptId))
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' has an empty scriptId.");
            }

            var configurationJson = "{}";

            if (root.TryGetProperty(
                    "defaultConfiguration",
                    out var configurationElement))
            {
                if (configurationElement.ValueKind !=
                    JsonValueKind.Object)
                {
                    throw new InvalidDataException(
                        $"The defaultConfiguration in '{path}' must be a JSON object.");
                }

                configurationJson =
                    JsonSerializer.Serialize(
                        configurationElement);
            }

            return new ScriptDefinition(
                formatVersion,
                scriptId,
                configurationJson);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Script definition '{path}' contains invalid JSON.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                $"Script definition '{path}' is invalid.",
                exception);
        }
    }
}