using System.Text;
using System.Text.Json;
using Engine.Core.Assets;
using Engine.Scripting;

namespace Engine.Serialization.Content;

public static class JsonScriptDefinitionSerializer
{
    private static readonly JsonSerializerOptions WriteOptions =
        new()
        {
            WriteIndented = true
        };

    public static ScriptDefinition Deserialize(
        AssetPath path,
        ReadOnlyMemory<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            throw new InvalidDataException(
                $"Script definition '{path}' is empty.");
        }

        try
        {
            var json = Encoding.UTF8.GetString(utf8Json.Span);

            if (json.Length > 0 && json[0] == '\uFEFF')
            {
                json = json[1..];
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' must be a JSON object.");
            }

            if (!root.TryGetProperty("formatVersion", out var versionElement) ||
                !versionElement.TryGetInt32(out var formatVersion))
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' has no valid formatVersion.");
            }

            if (formatVersion != ScriptDefinition.CurrentFormatVersion)
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' uses unsupported format version " +
                    $"'{formatVersion}'.");
            }

            if (!root.TryGetProperty("scriptId", out var idElement) ||
                idElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' has no valid scriptId.");
            }

            var scriptId = idElement.GetString();

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
                if (configurationElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException(
                        $"The defaultConfiguration in '{path}' must be a JSON object.");
                }

                configurationJson =
                    JsonSerializer.Serialize(configurationElement);
            }

            var assemblyName =
                ReadOptionalString(root, "implementationAssembly", path);

            var typeName =
                ReadOptionalString(root, "implementationType", path);

            if ((assemblyName is null) != (typeName is null))
            {
                throw new InvalidDataException(
                    $"Script definition '{path}' must specify both " +
                    "'implementationAssembly' and 'implementationType'.");
            }

            return new ScriptDefinition(
                formatVersion,
                scriptId,
                configurationJson,
                assemblyName,
                typeName);
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

    public static byte[] Serialize(ScriptDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(
                   stream,
                   new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            writer.WriteNumber("formatVersion", definition.FormatVersion);
            writer.WriteString("scriptId", definition.ScriptId);

            if (definition.HasImplementation)
            {
                writer.WriteString(
                    "implementationAssembly",
                    definition.ImplementationAssemblyName);

                writer.WriteString(
                    "implementationType",
                    definition.ImplementationTypeName);
            }

            writer.WritePropertyName("defaultConfiguration");

            using var configuration =
                JsonDocument.Parse(definition.DefaultConfigurationJson);

            configuration.RootElement.WriteTo(writer);

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static string? ReadOptionalString(
        JsonElement root,
        string propertyName,
        AssetPath path)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(
                $"Property '{propertyName}' in '{path}' must be a string.");
        }

        var value = element.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"Property '{propertyName}' in '{path}' cannot be empty.");
        }

        return value;
    }
}