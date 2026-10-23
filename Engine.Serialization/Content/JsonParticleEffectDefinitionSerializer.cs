using System.Text.Json;
using Engine.Core.Assets;
using Engine.Particles;

namespace Engine.Serialization.Content;

public static class JsonParticleEffectDefinitionSerializer
{
    private static readonly JsonSerializerOptions WriteOptions =
        new()
        {
            WriteIndented = true
        };

    public static ParticleEffectDefinition Deserialize(
        AssetPath path,
        ReadOnlyMemory<byte> utf8Json)
    {
        return JsonParticleEffectContentLoader.ParseDefinition(
            path,
            utf8Json);
    }

    public static byte[] Serialize(
        ParticleEffectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var json = new
        {
            formatVersion = definition.FormatVersion,
            texturePath = definition.TexturePath,
            emissionRatePerSecond =
                definition.EmissionRatePerSecond,
            maxParticles = definition.MaxParticles,
            layer = definition.Layer,

            particle = new
            {
                lifetimeSeconds = new
                {
                    min = definition.LifetimeSeconds.Min,
                    max = definition.LifetimeSeconds.Max
                },

                size = new
                {
                    min = definition.Size.Min,
                    max = definition.Size.Max
                },

                velocity = new
                {
                    min = new
                    {
                        x = definition.Velocity.Min.X,
                        y = definition.Velocity.Min.Y
                    },

                    max = new
                    {
                        x = definition.Velocity.Max.X,
                        y = definition.Velocity.Max.Y
                    }
                }
            }
        };

        return JsonSerializer.SerializeToUtf8Bytes(
            json,
            WriteOptions);
    }
}