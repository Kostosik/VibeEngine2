using Engine.ECS.Components;
using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Editor.UI.Dialogs;
using Engine.Editor.UI.Workspace;
using Engine.Graphics.Tilemaps;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class TilemapInspectorTests
{
    [Fact]
    public void PickerField_SelectsTilemapFromDialogAndSupportsUndoRedo()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineTilemapPicker-{Guid.NewGuid():N}");

        var mapsPath = Path.Combine(rootPath, "Maps");
        Directory.CreateDirectory(mapsPath);

        File.WriteAllText(
            Path.Combine(mapsPath, "Overworld.tilemap.json"),
            "{}");

        File.WriteAllText(
            Path.Combine(mapsPath, "notes.json"),
            "{}");

        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(entity, new WorldTransform2D());
        ecsWorld.Add(entity, new TilemapComponent());

        var editor = new EditorContext();

        editor.InitializeAssetBrowser(
            new FileSystemAssetSource(),
            rootPath);

        var document = editor.OpenDocument(world);

        var root = new UiRoot();
        var overlays = new UiOverlayLayer();
        root.AddChild(overlays);

        var dialog = new EditorAssetPickerDialog(
            editor,
            overlays,
            new UiFocusManager());

        try
        {
            document.EntitySelection.Set(entity);

            var panel = new InspectorPanelView(
                editor,
                assetPickerDialog: dialog);

            panel.Refresh();

            var picker = Descendants(panel)
                .OfType<UiPickerField>()
                .Single();

            picker.RequestSelection();

            Assert.True(dialog.IsOpen);

            FindButton(overlays, "[Folder] Maps").Click();

            Assert.Contains(
                Descendants(overlays).OfType<UiButton>(),
                button => button.Text == "Overworld.tilemap.json");

            Assert.DoesNotContain(
                Descendants(overlays).OfType<UiButton>(),
                button => button.Text == "notes.json");

            FindButton(overlays, "Overworld.tilemap.json").Click();

            Assert.Equal(
                "Maps/Overworld.tilemap.json",
                ecsWorld.Get<TilemapComponent>(entity).TilemapPath);

            document.Undo();

            Assert.Equal(
                string.Empty,
                ecsWorld.Get<TilemapComponent>(entity).TilemapPath);

            document.Redo();

            Assert.Equal(
                "Maps/Overworld.tilemap.json",
                ecsWorld.Get<TilemapComponent>(entity).TilemapPath);

            FindButton(panel, "Clear").Click();

            Assert.Equal(
                string.Empty,
                ecsWorld.Get<TilemapComponent>(entity).TilemapPath);

            document.Undo();

            Assert.Equal(
                "Maps/Overworld.tilemap.json",
                ecsWorld.Get<TilemapComponent>(entity).TilemapPath);
        }
        finally
        {
            dialog.Close();
            editor.Session.Discard(document);

            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    private static UiButton FindButton(
        UiWidget root,
        string text)
    {
        return Descendants(root)
            .OfType<UiButton>()
            .Single(button => button.Text == text);
    }

    private static IEnumerable<UiWidget> Descendants(
        UiWidget widget)
    {
        yield return widget;

        if (widget is not UiContainer container)
        {
            yield break;
        }

        foreach (var child in container.Children)
        {
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}