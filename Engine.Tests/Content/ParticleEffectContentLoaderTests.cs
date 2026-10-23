using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Particles;
using Engine.Serialization.Content;

namespace Engine.Tests.Content;

public sealed class ParticleEffectContentLoaderTests
{
    private const string ValidJson = """
    {
      "formatVersion": 1,
      "texturePath": "Textures\\Particles\\smoke.png",
      "emissionRatePerSecond": 12,
      "maxParticles": 128,
      "layer": 0,
      "particle": {
        "lifetimeSeconds": { "min": 0.8, "max": 1.4 },
        "size": { "min": 2, "max": 6 },
        "velocity": {
          "min": { "x": -8, "y": -24 },
          "max": { "x": 8, "y": -4 }
        }
      }
    }
    """;

    [Fact]
    public void Load_ParsesParticleEffectDefinition()
    {
        var definition = LoadDefinition(ValidJson);

        Assert.Equal(
            ParticleEffectDefinition.CurrentFormatVersion,
            definition.FormatVersion);

        Assert.Equal(
            "Textures/Particles/smoke.png",
            definition.TexturePath);

        Assert.Equal(
            12.0f,
            definition.EmissionRatePerSecond);

        Assert.Equal(128, definition.MaxParticles);
        Assert.Equal(0, definition.Layer);

        Assert.Equal(
            new ParticleFloatRange(0.8f, 1.4f),
            definition.LifetimeSeconds);

        Assert.Equal(
            new ParticleFloatRange(2.0f, 6.0f),
            definition.Size);

        Assert.Equal(
            new ParticleVector2Range(
                new Vector2(-8.0f, -24.0f),
                new Vector2(8.0f, -4.0f)),
            definition.Velocity);
    }

    [Fact]
    public void Load_RejectsUnsupportedFormatVersion()
    {
        var json = ValidJson.Replace(
            "\"formatVersion\": 1",
            "\"formatVersion\": 999",
            StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () => LoadDefinition(json));
    }

    [Fact]
    public void Load_RejectsInvalidConfigurationValues()
    {
        var invalidDocuments = new[]
        {
            ValidJson.Replace(
                "\"emissionRatePerSecond\": 12",
                "\"emissionRatePerSecond\": -1",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"emissionRatePerSecond\": 12",
                "\"emissionRatePerSecond\": 1e1000",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"maxParticles\": 128",
                "\"maxParticles\": 0",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"min\": 0.8, \"max\": 1.4",
                "\"min\": 0, \"max\": 1.4",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"min\": 0.8, \"max\": 1.4",
                "\"min\": 2, \"max\": 1",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"min\": 2, \"max\": 6",
                "\"min\": -1, \"max\": 6",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"min\": { \"x\": -8, \"y\": -24 }",
                "\"min\": { \"x\": 9, \"y\": -24 }",
                StringComparison.Ordinal)
        };

        foreach (var json in invalidDocuments)
        {
            Assert.Throws<InvalidDataException>(
                () => LoadDefinition(json));
        }
    }

    [Fact]
    public void Load_RejectsMissingRequiredFields()
    {
        Assert.Throws<InvalidDataException>(
            () => LoadDefinition(
                """{"formatVersion":1}"""));
    }

    [Fact]
    public void Load_RejectsMalformedJson()
    {
        Assert.Throws<InvalidDataException>(
            () => LoadDefinition("{ invalid json"));
    }

    private static ParticleEffectDefinition LoadDefinition(
        string json)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineParticleEffect-{Guid.NewGuid():N}");

        var particlesDirectory = Path.Combine(
            root,
            "Particles");

        Directory.CreateDirectory(particlesDirectory);

        try
        {
            File.WriteAllText(
                Path.Combine(
                    particlesDirectory,
                    "Smoke.particle.json"),
                json);

            using var content = new ContentManager(
                new FileAssetSource(root),
                new FileContentCatalog(root),
                new ContentLoaderRegistry());

            content.Register(
                new JsonParticleEffectContentLoader());

            return content.Load<ParticleEffectDefinition>(
                new AssetPath(
                    "Particles/Smoke.particle.json"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(
                    root,
                    recursive: true);
            }
        }
    }
}