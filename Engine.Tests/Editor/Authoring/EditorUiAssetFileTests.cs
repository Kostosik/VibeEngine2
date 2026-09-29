using Engine.Core.Math;
using Engine.Editor.UI.Authoring;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiAssetFileTests
{
    [Fact]
    public void SaveLoad_PreservesDocumentAndLoadedDocumentIsClean()
    {
        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.ui");

        try
        {
            var document =
                new EditorUiDocument(
                    "MainMenu");

            var panel =
                document.AddElement(
                    EditorUiElementType.Panel,
                    document.Root.Id,
                    "MainPanel");

            document.AddElement(
                EditorUiElementType.Button,
                panel.Id,
                "PlayButton");

            EditorUiAssetFile.Save(
                document,
                path);

            document.MarkSaved();

            var restored =
                EditorUiAssetFile.Load(
                    path);

            Assert.Equal(
                document.Name,
                restored.Name);

            Assert.Equal(
                document.Root.Id,
                restored.Root.Id);

            Assert.Equal(
                document.Root.Layout.Size,
                restored.Root.Layout.Size);

            Assert.False(
                restored.IsDirty);

            var restoredPanel =
                restored.GetElement(
                    panel.Id);

            Assert.Equal(
                panel.Id,
                restoredPanel.Id);

            Assert.Equal(
                restored.Root,
                restoredPanel.Parent);

            Assert.Equal(
                1,
                restoredPanel.Children.Count);

            Assert.Equal(
                "PlayButton",
                restoredPanel.Children[0].Name);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}