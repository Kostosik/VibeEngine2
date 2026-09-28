using Engine.Editor.UI.Authoring;
using Engine.UI.Assets;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiAssetConverterTests
{
    [Fact]
    public void Convert_PreservesDocumentStructure()
    {
        var document =
            new EditorUiDocument(
                "MainMenu");

        var panel =
            document.AddElement(
                EditorUiElementType.Panel,
                document.Root.Id,
                "MenuPanel");

        var button =
            document.AddElement(
                EditorUiElementType.Button,
                panel.Id,
                "StartButton");

        var asset =
            EditorUiAssetConverter.Convert(
                document);

        Assert.Equal(
            "MainMenu",
            asset.Name);

        Assert.Equal(
            document.Root.Layout.Size,
            asset.CanvasSize);

        Assert.Equal(
            3,
            asset.Elements.Count);

        Assert.Equal(
            document.Root.Id,
            asset.Root.Id);

        var assetPanel =
            asset.Elements[1];

        Assert.Equal(
            panel.Id,
            assetPanel.Id);

        Assert.Equal(
            document.Root.Id,
            assetPanel.ParentId);

        var assetButton =
            asset.Elements[2];

        Assert.Equal(
            button.Id,
            assetButton.Id);

        Assert.Equal(
            panel.Id,
            assetButton.ParentId);

        Assert.Equal(
            UiAssetElementType.Button,
            assetButton.Type);

        Assert.Equal(
            "StartButton",
            assetButton.Name);
    }
}