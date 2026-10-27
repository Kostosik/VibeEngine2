using Engine.Editor.Documents;
using Engine.Graphics2D.Sprites;

namespace Engine.Tests.Editor;

public sealed class SpriteAnimationDocumentTests
{
    [Fact]
    public void Open_LoadsDefinitionAndStartsClean()
    {
        var directory = Directory.CreateTempSubdirectory();

        try
        {
            var path = Path.Combine(
                directory.FullName,
                "Idle.sprite-animation.json");

            var serializer =
                new SpriteAnimationDefinitionJsonSerializer();

            File.WriteAllText(
                path,
                serializer.Serialize(
                    new SpriteAnimationDefinition(
                        new[]
                        {
                            new SpriteAnimationFrameDefinition(
                                "Textures/idle_0.png")
                        },
                        0.12f,
                        true)));

            var document = SpriteAnimationDocument.Open(path);

            Assert.Equal(
                SpriteAnimationDocument.TypeId,
                document.DocumentTypeId);

            Assert.Equal(
                Path.GetFullPath(path),
                document.FilePath);

            Assert.False(document.IsDirty);
            Assert.Single(document.Definition.Frames);
            Assert.Equal(
                "Textures/idle_0.png",
                document.Definition.Frames[0].TexturePath);
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
                "Walk.sprite-animation.json");

            var serializer =
                new SpriteAnimationDefinitionJsonSerializer();

            File.WriteAllText(
                path,
                serializer.Serialize(
                    new SpriteAnimationDefinition()));

            var document = SpriteAnimationDocument.Open(path);

            document.UpdateDefinition(
                new SpriteAnimationDefinition(
                    new[]
                    {
                        new SpriteAnimationFrameDefinition(
                            "Textures/walk_0.png"),
                        new SpriteAnimationFrameDefinition(
                            "Textures/walk_1.png")
                    },
                    0.08f,
                    false));

            Assert.True(document.IsDirty);

            document.Save();

            Assert.False(document.IsDirty);

            var reloaded = SpriteAnimationDocument.Open(path);

            Assert.Equal(2, reloaded.Definition.Frames.Count);
            Assert.Equal(
                "Textures/walk_1.png",
                reloaded.Definition.Frames[1].TexturePath);
            Assert.Equal(0.08f, reloaded.Definition.FrameDurationSeconds);
            Assert.False(reloaded.Definition.Loop);
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
                "Idle.json");

            File.WriteAllText(path, "{}");

            Assert.Throws<ArgumentException>(
                () => SpriteAnimationDocument.Open(path));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}