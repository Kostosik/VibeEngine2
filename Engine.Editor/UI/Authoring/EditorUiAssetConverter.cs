using Engine.Core.Math;
using Engine.UI.Assets;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

public static class EditorUiAssetConverter
{
    public static UiAsset Convert(
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        var elements =
            document
                .EnumerateDepthFirst()
                .Select(ConvertElement)
                .ToArray();

        return new UiAsset(
            document.Name,
            document.Root.Layout.Size,
            elements);
    }

    private static UiAssetElement ConvertElement(
        EditorUiElement element)
    {
        return new UiAssetElement(
            element.Id,
            element.Parent?.Id ?? Guid.Empty,
            ConvertType(element.Type),
            element.Name,
            new UiAssetLayout(
                new UiAnchor(
                    element.Layout.Anchor.X,
                    element.Layout.Anchor.Y),

                element.Layout.Offset,

                element.Layout.Size),

            element.Text,
            element.Texture,
            element.Action);
    }

    private static UiAssetElementType ConvertType(
        EditorUiElementType type)
    {
        return type switch
        {
            EditorUiElementType.Root =>
                UiAssetElementType.Root,

            EditorUiElementType.Panel =>
                UiAssetElementType.Panel,

            EditorUiElementType.Label =>
                UiAssetElementType.Label,

            EditorUiElementType.Button =>
                UiAssetElementType.Button,

            EditorUiElementType.Image =>
                UiAssetElementType.Image,

            EditorUiElementType.TextBox =>
                UiAssetElementType.TextBox,

            EditorUiElementType.Toggle =>
                UiAssetElementType.Toggle,

            EditorUiElementType.Dropdown =>
                UiAssetElementType.Dropdown,

            EditorUiElementType.ScrollView =>
                UiAssetElementType.ScrollView,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Unsupported editor UI element type.")
        };
    }
}