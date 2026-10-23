using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Editor.UI.Dialogs;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.Tests.Editor;

public sealed class EditorAssetPickerDialogTests
{
    [Fact]
    public void Show_SelectsMatchingAssetAndReturnsRelativePath()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineAssetPicker-{Guid.NewGuid():N}");

        var texturesPath = Path.Combine(
            rootPath,
            "Textures");

        Directory.CreateDirectory(texturesPath);

        File.WriteAllText(
            Path.Combine(texturesPath, "player.png"),
            string.Empty);

        File.WriteAllText(
            Path.Combine(texturesPath, "notes.txt"),
            string.Empty);

        var editor = new EditorContext();

        editor.InitializeAssetBrowser(
            new FileSystemAssetSource(),
            rootPath);

        var root = new UiRoot();
        var overlays = new UiOverlayLayer();

        root.AddChild(overlays);

        var dialog = new EditorAssetPickerDialog(
            editor,
            overlays,
            new UiFocusManager());

        string? selectedPath = null;

        try
        {
            Assert.True(
                dialog.Show(
                    "Select texture",
                    IsTextureAsset,
                    path => selectedPath = path));

            Assert.True(dialog.IsOpen);

            FindButton(root, "[Folder] Textures")
                .Click();

            Assert.True(dialog.IsOpen);

            Assert.Contains(
                Descendants(root).OfType<UiButton>(),
                button => button.Text == "player.png");

            Assert.DoesNotContain(
                Descendants(root).OfType<UiButton>(),
                button => button.Text == "notes.txt");

            FindButton(root, "player.png").Click();

            Assert.Equal(
                "Textures/player.png",
                selectedPath);

            Assert.False(dialog.IsOpen);
        }
        finally
        {
            dialog.Close();

            if (Directory.Exists(rootPath))
            {
                Directory.Delete(
                    rootPath,
                    recursive: true);
            }
        }
    }

    private static bool IsTextureAsset(
        EditorAssetEntry entry)
    {
        return Path.GetExtension(entry.Name).Equals(
            ".png",
            StringComparison.OrdinalIgnoreCase);
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