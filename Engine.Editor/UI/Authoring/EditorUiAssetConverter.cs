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
        var layout =
            new UiAssetLayout(
                new UiAnchor(
                    element.Layout.Anchor.X,
                    element.Layout.Anchor.Y),

                element.Layout.Offset,

                element.Layout.Size);

        return new UiAssetElement(
            element.Id,
            element.Parent?.Id ?? Guid.Empty,
            ConvertType(element.Type),
            element.Name,
            layout,

            element.Text,
            element.Texture,
            element.Action,
            element.DropdownOptions,
            element.SelectedIndex,
            element.ToggleValue,
            element.Placeholder,
            element.MaxLength,
            element.Visible,
            element.Enabled,
            element.ZIndex);
    }

    public static EditorUiDocument ConvertToDocument(
    UiAsset asset)
    {
        ArgumentNullException.ThrowIfNull(
            asset);

        var root =
            asset.Root;

        var document =
            new EditorUiDocument(
                asset.Name,
                root.Id,
                asset.CanvasSize);

        var descriptors =
            asset.Elements
                .Where(
                    static element =>
                        element.Type !=
                        UiAssetElementType.Root)
                .ToArray();

        var elements =
            new Dictionary<
                Guid,
                EditorUiElement>();

        foreach (var descriptor in descriptors)
        {
            var element =
                new EditorUiElement(
                    descriptor.Id,
                    ConvertType(
                        descriptor.Type),
                    descriptor.Name,
                    new EditorUiLayout(
                        new Vector2(
                            descriptor.Layout.Anchor.X,
                            descriptor.Layout.Anchor.Y),
                        descriptor.Layout.Offset,
                        descriptor.Layout.Size))
                {
                    Text =
                        descriptor.Text,

                    Texture =
                        descriptor.Texture,

                    Action =
                        descriptor.Action,

                    DropdownOptions =
                        descriptor.DropdownOptions.ToArray(),

                    SelectedIndex =
                        descriptor.SelectedIndex,

                    ToggleValue =
                        descriptor.ToggleValue,

                    Placeholder =
                        descriptor.Placeholder,

                    MaxLength =
                        descriptor.MaxLength,

                        Visible =
    descriptor.Visible,

                    Enabled =
    descriptor.Enabled,

                    ZIndex =
    descriptor.ZIndex
                };

            elements.Add(
                descriptor.Id,
                element);
        }

        var childrenByParent =
            descriptors
                .GroupBy(
                    static element =>
                        element.ParentId)
                .ToDictionary(
                    static group =>
                        group.Key,
                    static group =>
                        group.ToArray());

        AttachChildren(
            document,
            document.Root,
            document.Root.Id,
            childrenByParent,
            elements);

        document.MarkSaved();

        return document;
    }

    private static EditorUiElementType ConvertType(
    UiAssetElementType type)
    {
        return type switch
        {
            UiAssetElementType.Root =>
                EditorUiElementType.Root,

            UiAssetElementType.Panel =>
                EditorUiElementType.Panel,

            UiAssetElementType.Label =>
                EditorUiElementType.Label,

            UiAssetElementType.Button =>
                EditorUiElementType.Button,

            UiAssetElementType.Image =>
                EditorUiElementType.Image,

            UiAssetElementType.TextBox =>
                EditorUiElementType.TextBox,

            UiAssetElementType.Toggle =>
                EditorUiElementType.Toggle,

            UiAssetElementType.Dropdown =>
                EditorUiElementType.Dropdown,

            UiAssetElementType.ScrollView =>
                EditorUiElementType.ScrollView,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Unsupported UI asset element type.")
        };
    }

    private static void AttachChildren(
    EditorUiDocument document,
    EditorUiElement parent,
    Guid parentId,
    IReadOnlyDictionary<
        Guid,
        UiAssetElement[]> childrenByParent,
    IReadOnlyDictionary<
        Guid,
        EditorUiElement> elements)
    {
        if (!childrenByParent.TryGetValue(
                parentId,
                out var children))
        {
            return;
        }

        for (var i = 0;
             i < children.Length;
             i++)
        {
            var descriptor =
                children[i];

            if (!elements.TryGetValue(
                    descriptor.Id,
                    out var element))
            {
                throw new InvalidDataException(
                    $"UI element '{descriptor.Id}' could not be reconstructed.");
            }

            document.AddElementCore(
                element,
                parent.Id,
                i);

            AttachChildren(
                document,
                element,
                element.Id,
                childrenByParent,
                elements);
        }
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