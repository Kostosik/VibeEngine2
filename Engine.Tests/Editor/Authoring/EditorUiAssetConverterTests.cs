using Engine.Editor.UI.Authoring;
using Engine.UI.Assets;
using Engine.UI.Layout;
using Engine.Core.Math;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiAssetConverterTests
{
    [Fact]
    public void ConvertToDocument_ThenConvert_PreservesAsset()
    {
        var document =
            new EditorUiDocument(
                "Settings");

        var panel =
            document.AddElement(
                EditorUiElementType.Panel,
                document.Root.Id,
                "SettingsPanel");

        var button =
            document.AddElement(
                EditorUiElementType.Button,
                panel.Id,
                "Apply");

        var first =
            EditorUiAssetConverter.Convert(
                document);

        var restoredDocument =
            EditorUiAssetConverter.ConvertToDocument(
                first);

        var second =
            EditorUiAssetConverter.Convert(
                restoredDocument);

        Assert.Equal(
            first.Name,
            second.Name);

        Assert.Equal(
            first.CanvasSize,
            second.CanvasSize);

        Assert.Equal(
            first.Elements.Count,
            second.Elements.Count);

        for (var i = 0;
             i < first.Elements.Count;
             i++)
        {
            Assert.Equal(
                first.Elements[i].Id,
                second.Elements[i].Id);

            Assert.Equal(
                first.Elements[i].ParentId,
                second.Elements[i].ParentId);

            Assert.Equal(
                first.Elements[i].Type,
                second.Elements[i].Type);

            Assert.Equal(
                first.Elements[i].Name,
                second.Elements[i].Name);

            Assert.Equal(
                first.Elements[i].Layout,
                second.Elements[i].Layout);

            Assert.Equal(
                first.Elements[i].Text,
                second.Elements[i].Text);

            Assert.Equal(
                first.Elements[i].Texture,
                second.Elements[i].Texture);

            Assert.Equal(
                first.Elements[i].Action,
                second.Elements[i].Action);

            Assert.Equal(
                first.Elements[i].DropdownOptions,
                second.Elements[i].DropdownOptions);

            Assert.Equal(
                first.Elements[i].SelectedIndex,
                second.Elements[i].SelectedIndex);

            Assert.Equal(
                first.Elements[i].ToggleValue,
                second.Elements[i].ToggleValue);

            Assert.Equal(
                first.Elements[i].Placeholder,
                second.Elements[i].Placeholder);

            Assert.Equal(
                first.Elements[i].MaxLength,
                second.Elements[i].MaxLength);

            Assert.Equal(
                first.Elements[i].Visible,
                second.Elements[i].Visible);

            Assert.Equal(
                first.Elements[i].Enabled,
                second.Elements[i].Enabled);

            Assert.Equal(
                first.Elements[i].ZIndex,
                second.Elements[i].ZIndex);
        }

        Assert.Equal(
            button.Id,
            second.Elements[2].Id);
    }



    [Fact]
    public void ConvertToDocument_PreservesStructureAndDoesNotMarkDirty()
    {
        var rootId =
            Guid.NewGuid();

        var panelId =
            Guid.NewGuid();

        var buttonId =
            Guid.NewGuid();

        var asset =
            new UiAsset(
                "MainMenu",
                new Vector2(
                    1920.0f,
                    1080.0f),
                new[]
                {
                new UiAssetElement(
                    rootId,
                    Guid.Empty,
                    UiAssetElementType.Root,
                    "Root",
                    new UiAssetLayout(
                        UiAnchor.TopLeft,
                        Vector2.Zero,
                        new Vector2(
                            1920.0f,
                            1080.0f))),

                new UiAssetElement(
                    panelId,
                    rootId,
                    UiAssetElementType.Panel,
                    "MenuPanel",
                    new UiAssetLayout(
                        UiAnchor.Center,
                        new Vector2(
                            10.0f,
                            20.0f),
                        new Vector2(
                            500.0f,
                            400.0f))),

                new UiAssetElement(
                    buttonId,
                    panelId,
                    UiAssetElementType.Button,
                    "PlayButton",
                    new UiAssetLayout(
                        UiAnchor.TopLeft,
                        new Vector2(
                            25.0f,
                            30.0f),
                        new Vector2(
                            200.0f,
                            50.0f)),
                    "Play",
                    null,
                    "game.start")
                });

        var document =
            EditorUiAssetConverter.ConvertToDocument(
                asset);

        Assert.Equal(
            asset.Name,
            document.Name);

        Assert.Equal(
            asset.Root.Id,
            document.Root.Id);

        Assert.Equal(
            asset.CanvasSize,
            document.Root.Layout.Size);

        Assert.False(
            document.IsDirty);

        var panel =
            document.GetElement(
                panelId);

        var button =
            document.GetElement(
                buttonId);

        Assert.Equal(
            document.Root,
            panel.Parent);

        Assert.Equal(
            panel,
            button.Parent);

        Assert.Equal(
            "PlayButton",
            button.Name);

        Assert.Equal(
            "Play",
            button.Text);

        Assert.Equal(
            "game.start",
            button.Action);

        Assert.Equal(
            new Vector2(
                25.0f,
                30.0f),
            button.Layout.Offset);
    }

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