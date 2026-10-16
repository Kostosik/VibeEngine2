using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Scripting;
using Engine.Serialization.Content;

namespace Engine.Tests.Content;

public sealed class ScriptDefinitionContentLoaderTests
{
    [Fact]
    public void Load_ReadsScriptDefinitionThroughContentManager()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        var scriptsDirectory =
            Path.Combine(root, "Scripts");

        Directory.CreateDirectory(
            scriptsDirectory);

        try
        {
            File.WriteAllText(
                Path.Combine(
                    scriptsDirectory,
                    "Movement.script.json"),
                """
                {
                  "formatVersion": 1,
                  "scriptId": "game.movement",
                  "defaultConfiguration": {
                    "speed": 3.5,
                    "enabled": true
                  }
                }
                """);

            var loaders =
                new ContentLoaderRegistry();

            using var content =
                new ContentManager(
                    new FileAssetSource(root),
                    new FileContentCatalog(root),
                    loaders);

            content.Register(
                new JsonScriptDefinitionContentLoader());

            var definition =
                content.Load<ScriptDefinition>(
                    new AssetPath(
                        "Scripts/Movement.script.json"));

            Assert.Equal(
                ScriptDefinition.CurrentFormatVersion,
                definition.FormatVersion);

            Assert.Equal(
                "game.movement",
                definition.ScriptId);

            Assert.Equal(
                """{"speed":3.5,"enabled":true}""",
                definition.DefaultConfigurationJson);
        }
        finally
        {
            Directory.Delete(
                root,
                recursive: true);
        }
    }

    [Fact]
    public void Load_RejectsUnsupportedFormatVersion()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(
                    root,
                    "Broken.script.json"),
                """
                {
                  "formatVersion": 999,
                  "scriptId": "game.movement"
                }
                """);

            var loaders =
                new ContentLoaderRegistry();

            using var content =
                new ContentManager(
                    new FileAssetSource(root),
                    new FileContentCatalog(root),
                    loaders);

            content.Register(
                new JsonScriptDefinitionContentLoader());

            Assert.Throws<InvalidDataException>(
                () => content.Load<ScriptDefinition>(
                    new AssetPath(
                        "Broken.script.json")));
        }
        finally
        {
            Directory.Delete(
                root,
                recursive: true);
        }
    }
}