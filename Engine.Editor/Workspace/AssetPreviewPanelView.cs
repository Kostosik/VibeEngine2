using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class AssetPreviewPanelView : UiPanel
{
    private readonly UiStackPanel _content;
    private readonly ITextureResourceManager _textures;
    private readonly EditorAssetInspector _inspector;

    private AssetPath? _loadedAssetPath;

    public AssetPreviewPanelView(
        EditorContext editor,
        ITextureResourceManager textures)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            textures);

        Editor = editor;
        _textures = textures;
        _inspector = new EditorAssetInspector();

        Background =
            new UiColor(
                28,
                28,
                28,
                255);

        Padding =
            new UiThickness(
                8.0f);

        _content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        AddChild(
            _content);
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
        _content.ClearChildren();

        var browser =
            Editor.AssetBrowser;

        var asset =
            browser?.SelectedAsset;

        if (asset is null)
        {
            UnloadPreview();

            _content.AddChild(
                new UiLabel(
                    "No asset selected")
                {
                    FontSize = 14.0f,
                    Color =
                        new UiColor(
                            160,
                            160,
                            160,
                            255)
                });

            return;
        }

        EditorAssetMetadata metadata;

        try
        {
            metadata =
                _inspector.Inspect(
                    asset);
        }
        catch (Exception exception)
        {
            UnloadPreview();

            AddError(
                exception.Message);

            return;
        }

        _content.AddChild(
            new UiLabel(
                "Asset Preview")
            {
                FontSize = 15.0f,

                Color =
                    new UiColor(
                        210,
                        210,
                        210,
                        255)
            });

        if (metadata.PreviewKind ==
            EditorAssetPreviewKind.Image)
        {
            AddImagePreview(
                metadata,
                browser!);
        }
        else
        {
            UnloadPreview();

            _content.AddChild(
                new UiLabel(
                    "Preview is not available for this asset type.")
                {
                    FontSize = 13.0f,

                    Color =
                        new UiColor(
                            150,
                            150,
                            150,
                            255)
                });
        }

        _content.AddChild(
            new UiSeparator());

        AddMetadata(
            metadata);
    }

    private void AddImagePreview(
        EditorAssetMetadata metadata,
        EditorAssetBrowser browser)
    {
        var relativePath =
            Path.GetRelativePath(
                browser.RootPath,
                metadata.Path);

        var assetPath =
            new AssetPath(
                relativePath);

        if (!_loadedAssetPath.HasValue ||
            _loadedAssetPath.Value != assetPath)
        {
            UnloadPreview();

            _textures.Load(
                assetPath);

            _loadedAssetPath =
                assetPath;
        }

        if (!_textures.TryGet(
                assetPath,
                out var texture) ||
            !_textures.TryGetDescription(
                assetPath,
                out var description))
        {
            UnloadPreview();

            AddError(
                "Failed to load image preview.");

            return;
        }

        var image =
            new UiImage(
                texture,
                new Vector2(
                    description.Width,
                    description.Height))
            {
                Height = 140.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top,

                PreserveAspectRatio = true
            };

        _content.AddChild(
            image);
    }

    private void AddMetadata(
        EditorAssetMetadata metadata)
    {
        _content.AddChild(
            CreateMetadataLabel(
                $"Name: {metadata.Name}"));

        _content.AddChild(
            CreateMetadataLabel(
                $"Type: {GetTypeName(metadata)}"));

        _content.AddChild(
            CreateMetadataLabel(
                $"Size: {FormatSize(metadata.SizeBytes)}"));

        _content.AddChild(
            CreateMetadataLabel(
                $"Path: {metadata.Path}"));
    }

    private static UiLabel CreateMetadataLabel(
        string text)
    {
        return new UiLabel(
            text)
        {
            FontSize = 12.0f,

            Color =
                new UiColor(
                    175,
                    175,
                    175,
                    255),

            HorizontalAlignment =
                UiHorizontalAlignment.Stretch
        };
    }

    private static string GetTypeName(
        EditorAssetMetadata metadata)
    {
        if (metadata.PreviewKind ==
            EditorAssetPreviewKind.Image)
        {
            return string.IsNullOrWhiteSpace(
                    metadata.Extension)
                ? "Image"
                : metadata.Extension[1..]
                    .ToUpperInvariant();
        }

        return string.IsNullOrWhiteSpace(
                metadata.Extension)
            ? "File"
            : metadata.Extension[1..]
                .ToUpperInvariant();
    }

    private static string FormatSize(
        long bytes)
    {
        const double kilobyte = 1024.0;
        const double megabyte = kilobyte * 1024.0;

        if (bytes >= megabyte)
        {
            return $"{bytes / megabyte:0.##} MB";
        }

        if (bytes >= kilobyte)
        {
            return $"{bytes / kilobyte:0.##} KB";
        }

        return $"{bytes} B";
    }

    private void AddError(
        string message)
    {
        _content.AddChild(
            new UiLabel(
                message)
            {
                FontSize = 12.0f,

                Color =
                    new UiColor(
                        220,
                        120,
                        120,
                        255)
            });
    }

    private void UnloadPreview()
    {
        if (!_loadedAssetPath.HasValue)
        {
            return;
        }

        _textures.Unload(
            _loadedAssetPath.Value);

        _loadedAssetPath = null;
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        _content.Measure(
            context,
            availableSize);

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var content =
            finalRect.Deflate(
                Padding);

        _content.Arrange(
            content);
    }
}