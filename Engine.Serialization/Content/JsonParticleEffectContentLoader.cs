using System.Text;
using System.Text.Json;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Particles;

namespace Engine.Serialization.Content;

public sealed class JsonParticleEffectContentLoader :
    IContentLoader<ParticleEffectDefinition>
{
    private const string FileSuffix = ".particle.json";

    public Type AssetType =>
        typeof(ParticleEffectDefinition);

    public bool CanLoad(ContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        return asset.Path.Value.EndsWith(
            FileSuffix,
            StringComparison.OrdinalIgnoreCase);
    }

    public ParticleEffectDefinition Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ParseDefinition(
            path,
            context.ReadBytes(path));
    }

    public async ValueTask<ParticleEffectDefinition> LoadAsync(
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

        return ParseDefinition(path, data);
    }

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(path, context);
    }

    public static ParticleEffectDefinition ParseDefinition(
        AssetPath path,
        ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new InvalidDataException(
                $"Particle effect '{path}' is empty.");
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
                    $"Particle effect '{path}' must be a JSON object.");
            }

            var formatVersion = ReadInt32(
                root,
                "formatVersion",
                "formatVersion",
                path);

            var texturePath = ReadString(
                root,
                "texturePath",
                "texturePath",
                path);

            var emissionRate = ReadSingle(
                root,
                "emissionRatePerSecond",
                "emissionRatePerSecond",
                path);

            var maxParticles = ReadInt32(
                root,
                "maxParticles",
                "maxParticles",
                path);

            var layer = ReadOptionalInt32(
                root,
                "layer",
                "layer",
                defaultValue: 0,
                path);

            var particle = ReadObject(
                root,
                "particle",
                "particle",
                path);

            var lifetime = ReadFloatRange(
                particle,
                "lifetimeSeconds",
                "particle.lifetimeSeconds",
                path);

            var size = ReadFloatRange(
                particle,
                "size",
                "particle.size",
                path);

            var velocity = ReadVector2Range(
                particle,
                "velocity",
                "particle.velocity",
                path);

            return new ParticleEffectDefinition(
                formatVersion,
                texturePath,
                emissionRate,
                maxParticles,
                layer,
                lifetime,
                size,
                velocity);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Particle effect '{path}' contains invalid JSON.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                $"Particle effect '{path}' contains invalid settings.",
                exception);
        }
    }

    private static JsonElement ReadProperty(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        if (!parent.TryGetProperty(
                propertyName,
                out var value))
        {
            throw new InvalidDataException(
                $"Particle effect '{path}' is missing " +
                $"required field '{fieldPath}'.");
        }

        return value;
    }

    private static JsonElement ReadObject(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            fieldPath,
            path);

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"Field '{fieldPath}' in particle effect '{path}' " +
                "must be an object.");
        }

        return value;
    }

    private static string ReadString(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            fieldPath,
            path);

        if (value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidDataException(
                $"Field '{fieldPath}' in particle effect '{path}' " +
                "must be a non-empty string.");
        }

        return value.GetString()!;
    }

    private static int ReadInt32(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            fieldPath,
            path);

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw new InvalidDataException(
                $"Field '{fieldPath}' in particle effect '{path}' " +
                "must be a 32-bit integer.");
        }

        return result;
    }

    private static int ReadOptionalInt32(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        int defaultValue,
        AssetPath path)
    {
        if (!parent.TryGetProperty(
                propertyName,
                out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw new InvalidDataException(
                $"Field '{fieldPath}' in particle effect '{path}' " +
                "must be a 32-bit integer.");
        }

        return result;
    }

    private static float ReadSingle(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        var value = ReadProperty(
            parent,
            propertyName,
            fieldPath,
            path);

        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetSingle(out var result) ||
            !float.IsFinite(result))
        {
            throw new InvalidDataException(
                $"Field '{fieldPath}' in particle effect '{path}' " +
                "must be a finite number.");
        }

        return result;
    }

    private static ParticleFloatRange ReadFloatRange(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        var range = ReadObject(
            parent,
            propertyName,
            fieldPath,
            path);

        return new ParticleFloatRange(
            ReadSingle(
                range,
                "min",
                $"{fieldPath}.min",
                path),
            ReadSingle(
                range,
                "max",
                $"{fieldPath}.max",
                path));
    }

    private static ParticleVector2Range ReadVector2Range(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        var range = ReadObject(
            parent,
            propertyName,
            fieldPath,
            path);

        return new ParticleVector2Range(
            ReadVector2(
                range,
                "min",
                $"{fieldPath}.min",
                path),
            ReadVector2(
                range,
                "max",
                $"{fieldPath}.max",
                path));
    }

    private static Vector2 ReadVector2(
        JsonElement parent,
        string propertyName,
        string fieldPath,
        AssetPath path)
    {
        var vector = ReadObject(
            parent,
            propertyName,
            fieldPath,
            path);

        return new Vector2(
            ReadSingle(
                vector,
                "x",
                $"{fieldPath}.x",
                path),
            ReadSingle(
                vector,
                "y",
                $"{fieldPath}.y",
                path));
    }
}