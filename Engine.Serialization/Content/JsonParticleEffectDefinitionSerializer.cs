using System.Text.Json;
using Engine.Core.Assets;
using Engine.Core.Math;
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

        object shape = definition.Emitter.Shape switch
        {
            PointParticleEmissionShapeDefinition =>
                new
                {
                    type = "point"
                },

            CircleParticleEmissionShapeDefinition circle =>
                new
                {
                    type = "circle",
                    radius = circle.Radius
                },

            RectangleParticleEmissionShapeDefinition rectangle =>
                new
                {
                    type = "rectangle",
                    size = new
                    {
                        x = rectangle.Size.X,
                        y = rectangle.Size.Y
                    }
                },

            _ => throw new NotSupportedException(
                $"Unsupported particle emission shape: " +
                definition.Emitter.Shape.GetType().FullName)
        };

        var modifiers = definition.Particle.Modifiers
            .Select(static modifier => modifier switch
            {
                AccelerationModifierDefinition acceleration =>
                    (object)new
                    {
                        type = "acceleration",
                        x = acceleration.Acceleration.X,
                        y = acceleration.Acceleration.Y
                    },

                DragModifierDefinition drag =>
                    new
                    {
                        type = "drag",
                        coefficient = drag.Coefficient
                    },

                SizeOverLifetimeModifierDefinition size =>
                    new
                    {
                        type = "sizeOverLifetime",
                        startMultiplier = size.StartMultiplier,
                        endMultiplier = size.EndMultiplier
                    },

                _ => throw new NotSupportedException(
                    $"Unsupported particle modifier: " +
                    modifier.GetType().FullName)
            })
            .ToArray();

        var json = new
        {
            formatVersion = definition.FormatVersion,
            texturePath = definition.TexturePath,
            layer = definition.Layer,

            emitter = new
            {
                ratePerSecond = definition.Emitter.RatePerSecond,
                maxParticles = definition.Emitter.MaxParticles,
                shape
            },

            particle = new
            {
                lifetimeSeconds = new
                {
                    min = definition.Particle.LifetimeSeconds.Min,
                    max = definition.Particle.LifetimeSeconds.Max
                },

                size = new
                {
                    min = definition.Particle.Size.Min,
                    max = definition.Particle.Size.Max
                },

                velocity = new
                {
                    min = new
                    {
                        x = definition.Particle.Velocity.Min.X,
                        y = definition.Particle.Velocity.Min.Y
                    },

                    max = new
                    {
                        x = definition.Particle.Velocity.Max.X,
                        y = definition.Particle.Velocity.Max.Y
                    }
                },

                modifiers
            }
        };

        return JsonSerializer.SerializeToUtf8Bytes(
            json,
            WriteOptions);
    }
}