using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.UI.Assets;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

internal sealed class EditorUiCanvasImageRenderer
{
    private readonly ITextureResourceManager? _textures;
    private readonly EditorContext? _editor;
    private readonly ITextureManager? _projectTextureManager;

    private readonly HashSet<AssetPath> _unavailableTextures = new();

    private readonly ImageTextureLoader _projectImageLoader = new();

    private readonly Dictionary<string, TextureHandle> _projectTextures =
        new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

    private readonly HashSet<string> _unavailableProjectTextures =
        new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

    private string? _projectAssetsRoot;

    public EditorUiCanvasImageRenderer(
        ITextureResourceManager? textures,
        EditorContext? editor,
        ITextureManager? projectTextureManager)
    {
        _textures = textures;
        _editor = editor;
        _projectTextureManager = projectTextureManager;
    }

    public void Draw(
        UiRenderContext context,
        EditorUiElement element,
        UiRect rect)
    {
        if (element.Texture is not AssetPath path ||
            string.IsNullOrWhiteSpace(path.Value))
        {
            DrawImagePlaceholder(context, rect, "No texture");
            return;
        }

        // Project assets take precedence over editor resources.
        if (_editor?.ActiveGameProject is { } project)
        {
            DrawProjectImage(
                context,
                element,
                path,
                rect,
                project.AssetsDirectoryPath);

            return;
        }

        if (_textures is null)
        {
            DrawImagePlaceholder(
                context,
                rect,
                "No texture manager");

            return;
        }

        if (_unavailableTextures.Contains(path))
        {
            DrawImagePlaceholder(
                context,
                rect,
                "Texture unavailable");

            return;
        }

        try
        {
            if (!_textures.TryGet(path, out var texture))
            {
                texture = _textures.Load(path);
            }

            if (!texture.IsValid)
            {
                _unavailableTextures.Add(path);

                DrawImagePlaceholder(
                    context,
                    rect,
                    "Texture unavailable");

                return;
            }

            var destination = rect;

            if (element.PreserveAspectRatio &&
                _textures.TryGetDescription(
                    path,
                    out var description) &&
                description.Width > 0 &&
                description.Height > 0 &&
                rect.Width > 0.0f &&
                rect.Height > 0.0f)
            {
                destination = CalculateAspectRatioRect(
                    rect,
                    description.Width,
                    description.Height);
            }

            context.DrawTexture(
                texture,
                destination,
                new Rectangle(0.0f, 0.0f, 1.0f, 1.0f));
        }
        catch
        {
            _unavailableTextures.Add(path);

            DrawImagePlaceholder(
                context,
                rect,
                "Texture unavailable");
        }
    }

    private void DrawProjectImage(
        UiRenderContext context,
        EditorUiElement element,
        AssetPath assetPath,
        UiRect rect,
        string assetsRoot)
    {
        if (_projectTextureManager is null)
        {
            DrawImagePlaceholder(
                context,
                rect,
                "Project texture manager unavailable");

            return;
        }

        string? fullPath = null;

        try
        {
            EnsureProjectAssetsRoot(assetsRoot);

            fullPath =
                EditorProjectAssetPathResolver.Resolve(
                    assetsRoot,
                    assetPath.Value,
                    "UI texture");

            if (_unavailableProjectTextures.Contains(fullPath))
            {
                DrawImagePlaceholder(
                    context,
                    rect,
                    "Texture unavailable");

                return;
            }

            if (!_projectTextures.TryGetValue(
                    fullPath,
                    out var texture) ||
                !_projectTextureManager.Exists(texture))
            {
                var imageData = _projectImageLoader.Load(
                    File.ReadAllBytes(fullPath));

                texture = _projectTextureManager.Create(imageData);

                if (!texture.IsValid ||
                    !_projectTextureManager.Exists(texture))
                {
                    throw new InvalidOperationException(
                        "Graphics backend failed to create the UI texture.");
                }

                _projectTextures[fullPath] = texture;
            }

            var description =
                _projectTextureManager.GetDescription(texture);

            var destination = rect;

            if (element.PreserveAspectRatio &&
                description.Width > 0 &&
                description.Height > 0 &&
                rect.Width > 0.0f &&
                rect.Height > 0.0f)
            {
                destination = CalculateAspectRatioRect(
                    rect,
                    description.Width,
                    description.Height);
            }

            context.DrawTexture(
                texture,
                destination,
                new Rectangle(0.0f, 0.0f, 1.0f, 1.0f));
        }
        catch
        {
            if (fullPath is not null)
            {
                _unavailableProjectTextures.Add(fullPath);
            }

            DrawImagePlaceholder(
                context,
                rect,
                "Texture unavailable");
        }
    }

    private void EnsureProjectAssetsRoot(string rootPath)
    {
        var fullRoot = Path.GetFullPath(rootPath);

        if (_projectAssetsRoot is not null &&
            PathsEqual(_projectAssetsRoot, fullRoot))
        {
            return;
        }

        ReleaseProjectTextures();

        _projectAssetsRoot = fullRoot;
    }
    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    private static UiRect CalculateAspectRatioRect(
        UiRect rect,
        int imageWidth,
        int imageHeight)
    {
        var scale = MathF.Min(
            rect.Width / imageWidth,
            rect.Height / imageHeight);

        var width = imageWidth * scale;
        var height = imageHeight * scale;

        return new UiRect(
            rect.X + (rect.Width - width) * 0.5f,
            rect.Y + (rect.Height - height) * 0.5f,
            width,
            height);
    }

    public void ReleaseResources()
    {
        ReleaseProjectTextures();
        _unavailableTextures.Clear();
        _projectAssetsRoot = null;
    }

    private void ReleaseProjectTextures()
    {
        if (_projectTextureManager is not null)
        {
            foreach (var texture in _projectTextures.Values)
            {
                if (_projectTextureManager.Exists(texture))
                {
                    _projectTextureManager.Destroy(texture);
                }
            }
        }

        _projectTextures.Clear();
        _unavailableProjectTextures.Clear();
    }

    private static void DrawImagePlaceholder(
        UiRenderContext context,
        UiRect rect,
        string message)
    {
        if (rect.Width < 1.0f ||
            rect.Height < 1.0f)
        {
            return;
        }

        var textSize = context.MeasureText(
            message,
            10.0f);

        var textPosition = UiTextLayout.CalculatePosition(
            rect,
            textSize,
            UiTextHorizontalAlignment.Center,
            UiTextVerticalAlignment.Center);

        context.DrawText(
            message,
            textPosition,
            10.0f,
            new UiColor(210, 170, 170, 255),
            layer: 2);
    }
}