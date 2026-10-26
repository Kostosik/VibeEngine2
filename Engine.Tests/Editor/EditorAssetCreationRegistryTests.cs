using System.Text.Json;
using Engine.Editor.Assets;

namespace Engine.Tests.Editor;

public sealed class EditorAssetCreationRegistryTests
{
    [Fact]
    public void CreateDefault_RegistersInitialCreationTypes()
    {
        var registry = EditorAssetCreationRegistry.CreateDefault();

        Assert.Equal(
            new[]
            {
        "folder",
        "particle-effect",
        "script-definition",
        "world-document",
        "ui-document"
            },
            registry.Definitions.Select(item => item.Id));

        Assert.Equal(
            ".particle.json",
            registry.GetRequired("particle-effect").Extension);

        Assert.Equal(
            ".script.json",
            registry.GetRequired("script-definition").Extension);
    }

    [Fact]
    public void Register_RejectsDuplicateId()
    {
        var registry = EditorAssetCreationRegistry.CreateDefault();

        var duplicate = new EditorAssetCreationDefinition(
            id: "FOLDER",
            displayName: "Another Folder",
            defaultName: "Another Folder",
            kind: EditorAssetCreationKind.Directory,
            extension: string.Empty);

        Assert.Throws<InvalidOperationException>(
            () => registry.Register(duplicate));
    }

    [Fact]
    public void ParticleEffectTemplate_IsValidJsonWithRequiredFields()
    {
        var definition =
            EditorAssetCreationRegistry.CreateDefault()
                .GetRequired("particle-effect");

        var contents = definition.CreateInitialContents("Smoke");

        using var json = JsonDocument.Parse(contents);

        var root = json.RootElement;

        Assert.Equal(
            2,
            root.GetProperty("formatVersion").GetInt32());

        Assert.True(root.TryGetProperty("emitter", out _));

        Assert.True(
            root.GetProperty("particle")
                .TryGetProperty("modifiers", out var modifiers));

        Assert.Equal(JsonValueKind.Array, modifiers.ValueKind);

        Assert.Equal(
            "Textures/particle.png",
            root.GetProperty("texturePath").GetString());

        Assert.True(root.TryGetProperty("particle", out _));
    }

    [Fact]
    public void ScriptDefinitionTemplate_UsesRequestedNameForScriptId()
    {
        var definition =
            EditorAssetCreationRegistry.CreateDefault()
                .GetRequired("script-definition");

        var contents = definition.CreateInitialContents("Player Movement");

        using var json = JsonDocument.Parse(contents);

        Assert.Equal(
            "game.player_movement",
            json.RootElement.GetProperty("scriptId").GetString());

        Assert.Equal(
            1,
            json.RootElement.GetProperty("formatVersion").GetInt32());
    }

    [Fact]
    public void CreationService_CreatesAndSelectsAssetFromDefinition()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var browser = new EditorAssetBrowser(
                new FileSystemAssetSource(),
                root.FullName);

            var service = new EditorAssetCreationService(browser);

            var definition =
                EditorAssetCreationRegistry.CreateDefault()
                    .GetRequired("particle-effect");

            var path = service.Create(definition, "Smoke");

            Assert.True(File.Exists(path));
            Assert.Equal("Smoke.particle.json", Path.GetFileName(path));
            Assert.Equal(path, browser.SelectedAsset?.Path);

            using var json = JsonDocument.Parse(File.ReadAllText(path));

            Assert.Equal(
                2,
                json.RootElement.GetProperty("formatVersion").GetInt32());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void CreationService_CreatesDirectoryFromDefinition()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var browser = new EditorAssetBrowser(
                new FileSystemAssetSource(),
                root.FullName);

            var service = new EditorAssetCreationService(browser);

            var definition =
                EditorAssetCreationRegistry.CreateDefault()
                    .GetRequired("folder");

            var path = service.Create(definition, "Custom Folder");

            Assert.True(Directory.Exists(path));
            Assert.Equal(path, browser.SelectedEntry?.Path);
            Assert.True(browser.SelectedEntry!.IsDirectory);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}