using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Particles;
using Engine.Serialization.Content;

namespace Engine.Tests.Content;

public sealed class ParticleEffectDefinitionJsonSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_PreservesAllFields()
    {
        var expected = CreateDefinition(
            new RectangleParticleEmissionShapeDefinition(
                new Vector2(64.0f, 32.0f)));

        var bytes =
            JsonParticleEffectDefinitionSerializer.Serialize(
                expected);

        var actual =
            JsonParticleEffectDefinitionSerializer.Deserialize(
                new AssetPath("Particles/Smoke.particle.json"),
                bytes);

        Assert.Equal(expected.FormatVersion, actual.FormatVersion);
        Assert.Equal(expected.TexturePath, actual.TexturePath);
        Assert.Equal(expected.Layer, actual.Layer);

        Assert.Equal(
            expected.Emitter.RatePerSecond,
            actual.Emitter.RatePerSecond);

        Assert.Equal(
            expected.Emitter.MaxParticles,
            actual.Emitter.MaxParticles);

        Assert.Equal(
            expected.Emitter.Shape,
            actual.Emitter.Shape);

        Assert.Equal(
            expected.Particle.LifetimeSeconds,
            actual.Particle.LifetimeSeconds);

        Assert.Equal(
            expected.Particle.Size,
            actual.Particle.Size);

        Assert.Equal(
            expected.Particle.Velocity,
            actual.Particle.Velocity);

        Assert.Equal(
            expected.Particle.Modifiers.Count,
            actual.Particle.Modifiers.Count);

        for (var i = 0; i < expected.Particle.Modifiers.Count; i++)
        {
            Assert.Equal(
                expected.Particle.Modifiers[i],
                actual.Particle.Modifiers[i]);
        }
    }

    [Fact]
    public void SerializeAndDeserialize_SupportsAllEmissionShapes()
    {
        ParticleEmissionShapeDefinition[] shapes =
        [
            new PointParticleEmissionShapeDefinition(),
            new CircleParticleEmissionShapeDefinition(12.0f),
            new RectangleParticleEmissionShapeDefinition(
                new Vector2(40.0f, 20.0f))
        ];

        foreach (var shape in shapes)
        {
            var expected = CreateDefinition(shape);

            var bytes =
                JsonParticleEffectDefinitionSerializer.Serialize(
                    expected);

            var actual =
                JsonParticleEffectDefinitionSerializer.Deserialize(
                    new AssetPath("Particles/Test.particle.json"),
                    bytes);

            Assert.Equal(shape, actual.Emitter.Shape);
        }
    }

    private static ParticleEffectDefinition CreateDefinition(
        ParticleEmissionShapeDefinition shape)
    {
        return new ParticleEffectDefinition(
            ParticleEffectDefinition.CurrentFormatVersion,
            "Textures/Particles/Smoke.png",
            layer: 3,
            emitter: new ParticleEmitterDefinition(
                ratePerSecond: 17.5f,
                maxParticles: 256,
                shape: shape),
            particle: new ParticleSpawnDefinition(
                new ParticleFloatRange(0.2f, 1.3f),
                new ParticleFloatRange(1.0f, 7.0f),
                new ParticleVector2Range(
                    new Vector2(-4.0f, -12.0f),
                    new Vector2(8.0f, 2.0f)),
                new ParticleModifierDefinition[]
                {
                    new AccelerationModifierDefinition(
                        new Vector2(0.0f, -18.0f)),
                    new DragModifierDefinition(0.4f),
                    new SizeOverLifetimeModifierDefinition(
                        1.0f,
                        0.0f)
                }));
    }
}