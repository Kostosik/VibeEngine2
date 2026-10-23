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
        var expected = new ParticleEffectDefinition(
            ParticleEffectDefinition.CurrentFormatVersion,
            "Textures/Particles/Smoke.png",
            17.5f,
            256,
            3,
            new ParticleFloatRange(0.2f, 1.3f),
            new ParticleFloatRange(1.0f, 7.0f),
            new ParticleVector2Range(
                new Vector2(-4.0f, -12.0f),
                new Vector2(8.0f, 2.0f)));

        var bytes =
            JsonParticleEffectDefinitionSerializer.Serialize(
                expected);

        var actual =
            JsonParticleEffectDefinitionSerializer.Deserialize(
                new AssetPath("Particles/Smoke.particle.json"),
                bytes);

        Assert.Equal(expected.FormatVersion, actual.FormatVersion);
        Assert.Equal(expected.TexturePath, actual.TexturePath);
        Assert.Equal(
            expected.EmissionRatePerSecond,
            actual.EmissionRatePerSecond);
        Assert.Equal(expected.MaxParticles, actual.MaxParticles);
        Assert.Equal(expected.Layer, actual.Layer);
        Assert.Equal(expected.LifetimeSeconds, actual.LifetimeSeconds);
        Assert.Equal(expected.Size, actual.Size);
        Assert.Equal(expected.Velocity, actual.Velocity);
    }
}