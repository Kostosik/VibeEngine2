using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor.Assets;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class AssetPreviewPanelView : UiPanel
{
    private readonly UiStackPanel _content;
    private readonly ITextureManager? _textureManager;
    private readonly ImageTextureLoader _imageLoader = new();
    private readonly EditorAssetInspector _inspector;

    private string? _loadedPreviewPath;
    private TextureHandle? _loadedPreviewTexture;

    public AssetPreviewPanelView(
        EditorContext editor,
        ITextureManager? textureManager)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        Editor = editor;
        _textureManager = textureManager;
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

    public void ReleaseResources()
    {
        UnloadPreview();
    }

    public void Refresh()
    {
        _content.ClearChildren();

        var browser =
            Editor.AssetBrowser;

        if (browser?.SelectedEntry is
            { IsDirectory: true } directory)
        {
            UnloadPreview();

            _content.AddChild(
                new UiLabel("Folder Preview")
                {
                    FontSize = 15.0f,
                    Color = new UiColor(
                        210, 210, 210, 255)
                });

            _content.AddChild(
                CreateMetadataLabel(
                    $"Name: {directory.Name}"));

            _content.AddChild(
                CreateMetadataLabel(
                    "Type: Folder"));

            _content.AddChild(
                CreateMetadataLabel(
                    $"Path: {directory.Path}"));

            return;
        }

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
            AddImagePreview(metadata);
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
    EditorAssetMetadata metadata)
    {
        if (_textureManager is null)
        {
            UnloadPreview();

            AddError(
                "Image preview texture manager is not configured.");

            return;
        }

        var fullPath = Path.GetFullPath(metadata.Path);

        try
        {
            var samePath =
                string.Equals(
                    _loadedPreviewPath,
                    fullPath,
                    OperatingSystem.IsWindows()
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal);

            var textureIsValid =
                _loadedPreviewTexture.HasValue &&
                _textureManager.Exists(
                    _loadedPreviewTexture.Value);

            if (!samePath || !textureIsValid)
            {
                UnloadPreview();

                var imageData =
                    _imageLoader.Load(
                        File.ReadAllBytes(fullPath));

                var texture =
                    _textureManager.Create(imageData);

                if (!texture.IsValid ||
                    !_textureManager.Exists(texture))
                {
                    throw new InvalidOperationException(
                        "Graphics backend failed to create the preview texture.");
                }

                _loadedPreviewPath = fullPath;
                _loadedPreviewTexture = texture;
            }

            var loadedTexture =
                _loadedPreviewTexture!.Value;

            var description =
                _textureManager.GetDescription(
                    loadedTexture);

            var image =
                new UiImage(
                    loadedTexture,
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

            _content.AddChild(image);
        }
        catch (Exception exception)
        {
            UnloadPreview();

            AddError(
                $"Failed to load image preview: {exception.Message}");
        }
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
        $"Modified: {metadata.LastModified:yyyy-MM-dd HH:mm}"));

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
        if (_loadedPreviewTexture is { } texture &&
            _textureManager is not null &&
            _textureManager.Exists(texture))
        {
            _textureManager.Destroy(texture);
        }

        _loadedPreviewTexture = null;
        _loadedPreviewPath = null;
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