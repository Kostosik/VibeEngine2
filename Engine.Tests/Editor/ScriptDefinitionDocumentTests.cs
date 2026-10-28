using Engine.Editor.Documents;

namespace Engine.Tests.Editor;

public sealed class ScriptDefinitionDocumentTests
{
    [Fact]
    public void UpdateScriptId_SaveAndReopen_PreservesImplementationAndConfiguration()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.script.json");

        try
        {
            File.WriteAllText(
                path,
                """
            {
              "formatVersion": 1,
              "scriptId": "game.new_script",
              "implementationAssembly": "Game",
              "implementationType": "Game.PlayerController",
              "defaultConfiguration": {
                "speed": 5
              }
            }
            """);

            var document =
                ScriptDefinitionDocument.Open(path);

            document.UpdateScriptId(
                "game.player_controller");

            document.Save();

            var reopened =
                ScriptDefinitionDocument.Open(path);

            Assert.Equal(
                "game.player_controller",
                reopened.Definition.ScriptId);

            Assert.Equal(
                "Game",
                reopened.Definition.ImplementationAssemblyName);

            Assert.Equal(
                "Game.PlayerController",
                reopened.Definition.ImplementationTypeName);

            Assert.Equal(
                """{"speed":5}""",
                reopened.Definition.DefaultConfigurationJson);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void UpdateImplementation_SaveAndReopen_PreservesSelection()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(
            directory,
            "DoorTrigger.script.json");

        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "formatVersion": 1,
                  "scriptId": "game.door_trigger",
                  "defaultConfiguration": {
                    "enabled": true
                  }
                }
                """);

            var document = ScriptDefinitionDocument.Open(path);

            Assert.False(document.IsDirty);
            Assert.False(document.Definition.HasImplementation);

            document.UpdateImplementation(
                "Game.Sandbox",
                "Game.Scripts.DoorTriggerScript");

            Assert.True(document.IsDirty);

            document.Save();

            Assert.False(document.IsDirty);

            var reopened = ScriptDefinitionDocument.Open(path);

            Assert.Equal(
                "Game.Sandbox",
                reopened.Definition.ImplementationAssemblyName);

            Assert.Equal(
                "Game.Scripts.DoorTriggerScript",
                reopened.Definition.ImplementationTypeName);

            Assert.Equal(
                "game.door_trigger",
                reopened.Definition.ScriptId);

            Assert.Equal(
                """{"enabled":true}""",
                reopened.Definition.DefaultConfigurationJson);

            Assert.False(reopened.IsDirty);
        }
        finally
        {
            Directory.Delete(
                directory,
                recursive: true);
        }
    }
}