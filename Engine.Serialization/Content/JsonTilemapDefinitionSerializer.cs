using System.Text;
using System.Text.Json;
using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;

namespace Engine.Serialization.Content;

public static class JsonTilemapDefinitionSerializer
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public static TilemapDefinition Deserialize(
        AssetPath path,
        ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new InvalidDataException(
                $"Tilemap '{path}' is empty.");
        }

        var json = Encoding.UTF8.GetString(data.Span);

        if (json.Length > 0 && json[0] == '\uFEFF')
        {
            json = json[1..];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    $"Tilemap '{path}' must be a JSON object.");
            }

            var formatVersion = ReadInt32(root, "formatVersion", path);
            var tileSetPath = ReadString(root, "tileSetPath", path);
            var width = ReadInt32(root, "width", path);
            var height = ReadInt32(root, "height", path);
            var tilesElement = ReadArray(root, "tiles", path);

            var tiles = new List<uint>();
            var index = 0;

            foreach (var element in tilesElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Number ||
                    !element.TryGetUInt32(out var value))
                {
                    throw new InvalidDataException(
                        $"Field 'tiles[{index}]' in Tilemap '{path}' " +
                        "must be an unsigned 32-bit integer.");
                }

                tiles.Add(value);
                index++;
            }

            return new TilemapDefinition(
                formatVersion,
                tileSetPath,
                width,
                height,
                tiles);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Tilemap '{path}' contains invalid JSON.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                $"Tilemap '{path}' contains invalid settings.",
                exception);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException(
                $"Tilemap '{path}' contains dimensions that are too large.",
                exception);
        }
    }

    public static byte[] Serialize(TilemapDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var json = new
        {
            formatVersion = definition.FormatVersion,
            tileSetPath = definition.TileSetPath,
            width = definition.Width,
            height = definition.Height,
            tiles = definition.Tiles
        };

        return JsonSerializer.SerializeToUtf8Bytes(
            json,
            WriteOptions);
    }

    private static JsonElement ReadProperty(
        JsonElement parent,
        string propertyName,
        AssetPath path)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            throw new InvalidDataException(
                $"Tilemap '{path}' is missing required field " +
                $"'{propertyName}'.");
        }

        return value;
    }

    private static string ReadString(
        JsonElement parent,
        string propertyName,
        AssetPath path)
    {
        var value = ReadProperty(parent, propertyName, path);

        if (value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException(
                $"Field '{propertyName}' in Tilemap '{path}' " +
                "must be a non-empty string.");
        }

        return value.GetString()!;
    }

    private static int ReadInt32(
        JsonElement parent,
        string propertyName,
        AssetPath path)
    {
        var value = ReadProperty(parent, propertyName, path);

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw new InvalidDataException(
                $"Field '{propertyName}' in Tilemap '{path}' " +
                "must be a 32-bit integer.");
        }

        return result;
    }

    private static JsonElement ReadArray(
        JsonElement parent,
        string propertyName,
        AssetPath path)
    {
        var value = ReadProperty(parent, propertyName, path);

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Field '{propertyName}' in Tilemap '{path}' " +
                "must be an array.");
        }

        return value;
    }
}