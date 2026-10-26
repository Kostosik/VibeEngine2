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
      "formatVersion": 2,
      "texturePath": "Textures\\Particles\\smoke.png",
      "layer": 0,
      "emitter": {
        "ratePerSecond": 12,
        "maxParticles": 128,
        "shape": {
          "type": "point"
        }
      },
      "particle": {
        "lifetimeSeconds": { "min": 0.8, "max": 1.4 },
        "size": { "min": 2, "max": 6 },
        "velocity": {
          "min": { "x": -8, "y": -24 },
          "max": { "x": 8, "y": -4 }
        },
        "modifiers": []
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

        Assert.Equal(12.0f, definition.Emitter.RatePerSecond);
        Assert.Equal(128, definition.Emitter.MaxParticles);
        Assert.Equal(0, definition.Layer);

        Assert.IsType<PointParticleEmissionShapeDefinition>(
            definition.Emitter.Shape);

        Assert.Equal(
            new ParticleFloatRange(0.8f, 1.4f),
            definition.Particle.LifetimeSeconds);

        Assert.Equal(
            new ParticleFloatRange(2.0f, 6.0f),
            definition.Particle.Size);

        Assert.Equal(
            new ParticleVector2Range(
                new Vector2(-8.0f, -24.0f),
                new Vector2(8.0f, -4.0f)),
            definition.Particle.Velocity);

        Assert.Empty(definition.Particle.Modifiers);
    }

    [Fact]
    public void Load_ParsesCircleShapeAndModifiers()
    {
        var json = ValidJson
            .Replace(
                "\"type\": \"point\"",
                "\"type\": \"circle\", \"radius\": 14",
                StringComparison.Ordinal)
            .Replace(
                "\"modifiers\": []",
                """
                "modifiers": [
                  { "type": "acceleration", "x": 0, "y": -18 },
                  { "type": "drag", "coefficient": 0.4 },
                  {
                    "type": "sizeOverLifetime",
                    "startMultiplier": 1,
                    "endMultiplier": 0
                  }
                ]
                """,
                StringComparison.Ordinal);

        var definition = LoadDefinition(json);

        var circle = Assert.IsType<
            CircleParticleEmissionShapeDefinition>(
                definition.Emitter.Shape);

        Assert.Equal(14.0f, circle.Radius);

        Assert.Collection(
            definition.Particle.Modifiers,
            modifier =>
            {
                var acceleration = Assert.IsType<
                    AccelerationModifierDefinition>(modifier);

                Assert.Equal(
                    new Vector2(0.0f, -18.0f),
                    acceleration.Acceleration);
            },
            modifier =>
            {
                var drag = Assert.IsType<
                    DragModifierDefinition>(modifier);

                Assert.Equal(0.4f, drag.Coefficient);
            },
            modifier =>
            {
                var size = Assert.IsType<
                    SizeOverLifetimeModifierDefinition>(modifier);

                Assert.Equal(1.0f, size.StartMultiplier);
                Assert.Equal(0.0f, size.EndMultiplier);
            });
    }

    [Fact]
    public void Load_ParsesRectangleShape()
    {
        var json = ValidJson.Replace(
            "\"type\": \"point\"",
            """
            "type": "rectangle",
            "size": { "x": 64, "y": 32 }
            """,
            StringComparison.Ordinal);

        var definition = LoadDefinition(json);

        var rectangle = Assert.IsType<
            RectangleParticleEmissionShapeDefinition>(
                definition.Emitter.Shape);

        Assert.Equal(
            new Vector2(64.0f, 32.0f),
            rectangle.Size);
    }

    [Fact]
    public void Load_RejectsUnsupportedFormatVersion()
    {
        var json = ValidJson.Replace(
            "\"formatVersion\": 2",
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
                "\"ratePerSecond\": 12",
                "\"ratePerSecond\": -1",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"ratePerSecond\": 12",
                "\"ratePerSecond\": 1e1000",
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
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"type\": \"point\"",
                "\"type\": \"circle\", \"radius\": -1",
                StringComparison.Ordinal),

            ValidJson.Replace(
                "\"modifiers\": []",
                """
                "modifiers": [
                  { "type": "drag", "coefficient": -1 }
                ]
                """,
                StringComparison.Ordinal)
        };

        foreach (var json in invalidDocuments)
        {
            Assert.Throws<InvalidDataException>(
                () => LoadDefinition(json));
        }
    }

    [Fact]
    public void Load_RejectsUnknownEmissionShape()
    {
        var json = ValidJson.Replace(
            "\"type\": \"point\"",
            "\"type\": \"triangle\"",
            StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () => LoadDefinition(json));
    }

    [Fact]
    public void Load_RejectsUnknownModifier()
    {
        var json = ValidJson.Replace(
            "\"modifiers\": []",
            """
            "modifiers": [
              { "type": "unknown" }
            ]
            """,
            StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () => LoadDefinition(json));
    }

    [Fact]
    public void Load_RejectsMissingRequiredFields()
    {
        Assert.Throws<InvalidDataException>(
            () => LoadDefinition(
                """{"formatVersion":2}"""));
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