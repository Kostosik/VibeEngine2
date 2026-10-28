using Engine.Animation;
using Engine.Core.Assets;
using Engine.Editor.Assets;
using Engine.Graphics.Tilemaps;
using Engine.Graphics2D.Sprites;
using Engine.Serialization.Content;
using System.Text.Json;

namespace Engine.Tests.Editor;

public sealed class EditorAssetCreationRegistryTests
{
    [Fact]
    public void SpriteAnimationTemplate_CreatesValidResource()
    {
        var definition =
            EditorAssetCreationRegistry.CreateDefault()
                .GetRequired("sprite-animation");

        var contents = definition.CreateInitialContents(
            "New Sprite Animation");

        var serializer = new SpriteAnimationDefinitionJsonSerializer();
        var animation = serializer.Deserialize(contents);

        Assert.Equal(".sprite-animation.json", definition.Extension);
        Assert.Empty(animation.Frames);
        Assert.Equal(0.1f, animation.FrameDurationSeconds);
        Assert.True(animation.Loop);
    }
    [Fact]
    public void AnimationClipTemplate_CreatesValidAnimationResource()
    {
        var definition =
            EditorAssetCreationRegistry.CreateDefault()
                .GetRequired("animation-clip");

        var contents =
            definition.CreateInitialContents("Idle");

        var serializer = new AnimationClipJsonSerializer();
        var clip = serializer.Deserialize(contents);

        Assert.Equal(".animation.json", definition.Extension);
        Assert.Equal(1.0, clip.DurationSeconds);
        Assert.Equal(0, clip.TrackCount);
    }

    [Fact]
    public void TileSetTemplate_CreatesValidResource()
    {
        var definition =
            EditorAssetCreationRegistry.CreateDefault()
                .GetRequired("tile-set");

        var contents =
            definition.CreateInitialContents("World");

        var tileSet =
            JsonTileSetDefinitionSerializer.Deserialize(
                new AssetPath("World.tileset.json"),
                System.Text.Encoding.UTF8.GetBytes(contents));

        Assert.Equal(".tileset.json", definition.Extension);
        Assert.Equal(
            TileSetDefinition.CurrentFormatVersion,
            tileSet.FormatVersion);

        Assert.Empty(tileSet.Atlases);
        Assert.Empty(tileSet.Tiles);
    }

    [Fact]
    public void TilemapTemplate_CreatesValidResource()
    {
        var definition =
            EditorAssetCreationRegistry.CreateDefault()
                .GetRequired("tilemap");

        var contents =
            definition.CreateInitialContents("New Tilemap");

        var map =
            JsonTilemapDefinitionSerializer.Deserialize(
                new AssetPath("New Tilemap.tilemap.json"),
                System.Text.Encoding.UTF8.GetBytes(contents));

        Assert.Equal(".tilemap.json", definition.Extension);
        Assert.Equal(16, map.Width);
        Assert.Equal(16, map.Height);
        Assert.Equal(256, map.Tiles.Count);
        Assert.All(map.Tiles, tile => Assert.Equal(0u, tile));
    }

    [Fact]
    public void CreateDefault_RegistersInitialCreationTypes()
    {
        var registry = EditorAssetCreationRegistry.CreateDefault();

        Assert.Equal(
            new[]
            {
                "tile-set",
                "tilemap",
                "folder",
                "particle-effect",
                "animation-clip",
                "sprite-animation",
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