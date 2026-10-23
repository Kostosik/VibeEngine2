
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor.Documents;
using Engine.Particles;
using Engine.Serialization.Content;

namespace Engine.Tests.Editor;

public sealed class ParticleEffectDocumentTests
{
    [Fact]
    public void Open_LoadsDefinitionAndStartsClean()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var path = Path.Combine(
                directory.FullName,
                "Smoke.particle.json");

            File.WriteAllBytes(
                path,
                JsonParticleEffectDefinitionSerializer.Serialize(
                    CreateDefinition()));

            var document = ParticleEffectDocument.Open(path);

            Assert.Equal(
                ParticleEffectDocument.TypeId,
                document.DocumentTypeId);

            Assert.Equal(
                Path.GetFullPath(path),
                document.FilePath);

            Assert.Equal(
                "Smoke.particle.json",
                document.DisplayName);

            Assert.Equal(12.0f,
                document.Definition.EmissionRatePerSecond);

            Assert.False(document.IsDirty);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void UpdateDefinition_MarksDirtyAndSavePersistsChanges()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var path = Path.Combine(
                directory.FullName,
                "Smoke.particle.json");

            File.WriteAllBytes(
                path,
                JsonParticleEffectDefinitionSerializer.Serialize(
                    CreateDefinition()));

            var document = ParticleEffectDocument.Open(path);
            var changedCount = 0;

            document.Changed += () => changedCount++;

            var original = document.Definition;

            document.UpdateDefinition(
                new ParticleEffectDefinition(
                    original.FormatVersion,
                    original.TexturePath,
                    24.0f,
                    original.MaxParticles,
                    original.Layer,
                    original.LifetimeSeconds,
                    original.Size,
                    original.Velocity));

            Assert.True(document.IsDirty);
            Assert.Equal(1, changedCount);

            document.Save();

            Assert.False(document.IsDirty);
            Assert.Equal(2, changedCount);

            var reloaded =
                JsonParticleEffectDefinitionSerializer.Deserialize(
                    new AssetPath("Smoke.particle.json"),
                    File.ReadAllBytes(path));

            Assert.Equal(
                24.0f,
                reloaded.EmissionRatePerSecond);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Open_RejectsUnsupportedFileExtension()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var path = Path.Combine(
                directory.FullName,
                "Smoke.json");

            File.WriteAllText(path, "{}");

            Assert.Throws<ArgumentException>(
                () => ParticleEffectDocument.Open(path));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static ParticleEffectDefinition CreateDefinition() =>
        new(
            ParticleEffectDefinition.CurrentFormatVersion,
            "Textures/particle.png",
            12.0f,
            128,
            0,
            new ParticleFloatRange(0.8f, 1.4f),
            new ParticleFloatRange(2.0f, 6.0f),
            new ParticleVector2Range(
                new Vector2(-8.0f, -24.0f),
                new Vector2(8.0f, -4.0f)));
}
