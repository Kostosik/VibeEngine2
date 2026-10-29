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

        var json = StrictUtf8Text.Decode(path, "Particle effect", data.Span);

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

            var layer = ReadInt32(
                root,
                "layer",
                "layer",
                path);

            var emitterElement = ReadObject(
                root,
                "emitter",
                "emitter",
                path);

            var ratePerSecond = ReadSingle(
                emitterElement,
                "ratePerSecond",
                "emitter.ratePerSecond",
                path);

            var maxParticles = ReadInt32(
                emitterElement,
                "maxParticles",
                "emitter.maxParticles",
                path);

            var shapeElement = ReadObject(
                emitterElement,
                "shape",
                "emitter.shape",
                path);

            var shape = ReadEmissionShape(
                shapeElement,
                "emitter.shape",
                path);

            var particleElement = ReadObject(
                root,
                "particle",
                "particle",
                path);

            var lifetime = ReadFloatRange(
                particleElement,
                "lifetimeSeconds",
                "particle.lifetimeSeconds",
                path);

            var size = ReadFloatRange(
                particleElement,
                "size",
                "particle.size",
                path);

            var velocity = ReadVector2Range(
                particleElement,
                "velocity",
                "particle.velocity",
                path);

            var modifiers = ReadModifiers(
                particleElement,
                "modifiers",
                "particle.modifiers",
                path);

            var emitter = new ParticleEmitterDefinition(
                ratePerSecond,
                maxParticles,
                shape);

            var particle = new ParticleSpawnDefinition(
                lifetime,
                size,
                velocity,
                modifiers);

            return new ParticleEffectDefinition(
                formatVersion,
                texturePath,
                layer,
                emitter,
                particle);
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

    private static ParticleEmissionShapeDefinition ReadEmissionShape(
    JsonElement element,
    string fieldPath,
    AssetPath path)
    {
        var type = ReadString(
            element,
            "type",
            $"{fieldPath}.type",
            path);

        return type switch
        {
            "point" =>
                new PointParticleEmissionShapeDefinition(),

            "circle" =>
                new CircleParticleEmissionShapeDefinition(
                    ReadSingle(
                        element,
                        "radius",
                        $"{fieldPath}.radius",
                        path)),

            "rectangle" =>
                new RectangleParticleEmissionShapeDefinition(
                    ReadVector2(
                        element,
                        "size",
                        $"{fieldPath}.size",
                        path)),

            _ => throw new InvalidDataException(
                $"Unknown emission shape '{type}' " +
                $"in particle effect '{path}'.")
        };
    }

    private static IReadOnlyList<ParticleModifierDefinition>
        ReadModifiers(
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

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Field '{fieldPath}' in particle effect '{path}' " +
                "must be an array.");
        }

        var modifiers = new List<ParticleModifierDefinition>();
        var index = 0;

        foreach (var element in value.EnumerateArray())
        {
            var itemPath = $"{fieldPath}[{index}]";

            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    $"Modifier '{itemPath}' in particle effect '{path}' " +
                    "must be an object.");
            }

            modifiers.Add(
                ReadModifier(element, itemPath, path));

            index++;
        }

        return modifiers;
    }

    private static ParticleModifierDefinition ReadModifier(
        JsonElement element,
        string fieldPath,
        AssetPath path)
    {
        var type = ReadString(
            element,
            "type",
            $"{fieldPath}.type",
            path);

        return type switch
        {
            "acceleration" =>
                new AccelerationModifierDefinition(
                    new Vector2(
                        ReadSingle(
                            element,
                            "x",
                            $"{fieldPath}.x",
                            path),
                        ReadSingle(
                            element,
                            "y",
                            $"{fieldPath}.y",
                            path))),

            "drag" =>
                new DragModifierDefinition(
                    ReadSingle(
                        element,
                        "coefficient",
                        $"{fieldPath}.coefficient",
                        path)),

            "sizeOverLifetime" =>
                new SizeOverLifetimeModifierDefinition(
                    ReadSingle(
                        element,
                        "startMultiplier",
                        $"{fieldPath}.startMultiplier",
                        path),
                    ReadSingle(
                        element,
                        "endMultiplier",
                        $"{fieldPath}.endMultiplier",
                        path)),

            _ => throw new InvalidDataException(
                $"Unknown particle modifier '{type}' " +
                $"at '{fieldPath}' in '{path}'.")
        };
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