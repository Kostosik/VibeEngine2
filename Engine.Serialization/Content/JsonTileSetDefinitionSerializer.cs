using System.Text;
using System.Text.Json;
using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;

namespace Engine.Serialization.Content;

public static class JsonTileSetDefinitionSerializer
{
    private static readonly JsonSerializerOptions WriteOptions =
        new()
        {
            WriteIndented = true
        };

    public static TileSetDefinition Deserialize(
        AssetPath path,
        ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new InvalidDataException(
                $"TileSet '{path}' is empty.");
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
                    $"TileSet '{path}' must be a JSON object.");
            }

            var formatVersion = ReadInt32(
                root,
                "formatVersion",
                path);

            var atlasesElement = ReadArray(
                root,
                "atlases",
                path);

            var atlases = new List<TileSetAtlasDefinition>();
            var atlasIndex = 0;

            foreach (var element in atlasesElement.EnumerateArray())
            {
                var field = $"atlases[{atlasIndex}]";

                EnsureObject(element, field, path);

                atlases.Add(
                    new TileSetAtlasDefinition(
                        ReadString(element, "id", $"{field}.id", path),
                        ReadString(
                            element,
                            "texturePath",
                            $"{field}.texturePath",
                            path),
                        ReadInt32(
                            element,
                            "tileWidth",
                            $"{field}.tileWidth",
                            path),
                        ReadInt32(
                            element,
                            "tileHeight",
                            $"{field}.tileHeight",
                            path)));

                atlasIndex++;
            }

            var tilesElement = ReadArray(
                root,
                "tiles",
                path);

            var tiles = new List<TileSetTileDefinition>();
            var tileIndex = 0;

            foreach (var element in tilesElement.EnumerateArray())
            {
                var field = $"tiles[{tileIndex}]";

                EnsureObject(element, field, path);

                var id = ReadUInt32(
                    element,
                    "id",
                    $"{field}.id",
                    path);

                var name = ReadOptionalString(
                    element,
                    "name",
                    $"{field}.name",
                    $"Tile {id}",
                    path);

                tiles.Add(
                    new TileSetTileDefinition(
                        id,
                        ReadString(
                            element,
                            "atlasId",
                            $"{field}.atlasId",
                            path),
                        ReadInt32(
                            element,
                            "atlasIndex",
                            $"{field}.atlasIndex",
                            path),
                        name));

                tileIndex++;
            }

            return new TileSetDefinition(
                formatVersion,
                atlases,
                tiles);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"TileSet '{path}' contains invalid JSON.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                $"TileSet '{path}' contains invalid settings.",
                exception);
        }
    }

    private static string ReadOptionalString(
    JsonElement parent,
    string propertyName,
    string field,
    string defaultValue,
    AssetPath path)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException(
                $"Field '{field}' in TileSet '{path}' must be a non-empty string.");
        }

        return value.GetString()!;
    }

    public static byte[] Serialize(
        TileSetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var json = new
        {
            formatVersion = definition.FormatVersion,

            atlases = definition.Atlases
                .Select(atlas => new
                {
                    id = atlas.Id,
                    texturePath = atlas.TexturePath,
                    tileWidth = atlas.TileWidth,
                    tileHeight = atlas.TileHeight
                })
                .ToArray(),

            tiles = definition.Tiles
                .Select(tile => new
                {
                    id = tile.Id,
                    name = tile.Name,
                    atlasId = tile.AtlasId,
                    atlasIndex = tile.AtlasIndex
                })
                .ToArray()
        };

        return JsonSerializer.SerializeToUtf8Bytes(
            json,
            WriteOptions);
    }

    private static JsonElement ReadProperty(
        JsonElement parent,
        string propertyName,
        string field,
        AssetPath path)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            throw new InvalidDataException(
                $"TileSet '{path}' is missing required field '{field}'.");
        }

        return value;
    }

    private static JsonElement ReadArray(
        JsonElement parent,
        string propertyName,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            propertyName,
            path);

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Field '{propertyName}' in TileSet '{path}' must be an array.");
        }

        return value;
    }

    private static void EnsureObject(
        JsonElement element,
        string field,
        AssetPath path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"Field '{field}' in TileSet '{path}' must be an object.");
        }
    }

    private static string ReadString(
        JsonElement parent,
        string propertyName,
        string field,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            field,
            path);

        if (value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException(
                $"Field '{field}' in TileSet '{path}' must be a non-empty string.");
        }

        return value.GetString()!;
    }

    private static int ReadInt32(
        JsonElement parent,
        string propertyName,
        AssetPath path)
    {
        return ReadInt32(
            parent,
            propertyName,
            propertyName,
            path);
    }

    private static int ReadInt32(
        JsonElement parent,
        string propertyName,
        string field,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            field,
            path);

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw new InvalidDataException(
                $"Field '{field}' in TileSet '{path}' must be a 32-bit integer.");
        }

        return result;
    }

    private static uint ReadUInt32(
        JsonElement parent,
        string propertyName,
        string field,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            field,
            path);

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetUInt32(out var result))
        {
            throw new InvalidDataException(
                $"Field '{field}' in TileSet '{path}' must be an unsigned 32-bit integer.");
        }

        return result;
    }
}