using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Assets;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class SpriteAnimationPreviewView : UiPanel
{
    private readonly SpriteAnimationDocument _document;
    private readonly EditorContext _editor;
    private readonly ITextureManager? _textureManager;
    private readonly ImageTextureLoader _textureLoader = new();

    private readonly Dictionary<string, TextureHandle> _textures =
        new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

    private SpriteAnimationDefinition? _loadedDefinition;
    private SpriteAnimation? _animation;
    private string? _error;

    public SpriteAnimationPreviewView(
        SpriteAnimationDocument document,
        EditorContext editor,
        ITextureManager? textureManager)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(editor);

        _document = document;
        _editor = editor;
        _textureManager = textureManager;

        Height = 320.0f;
        Background = new UiColor(15, 17, 21, 255);

        HorizontalAlignment =
            UiHorizontalAlignment.Stretch;
    }

    protected override void OnUpdate(double deltaSeconds)
    {
        EnsureAnimation();

        if (_animation is null)
        {
            return;
        }

        // Ограничиваем скачок времени после паузы окна.
        var delta = (float)Math.Clamp(
            deltaSeconds,
            0.0,
            0.05);

        if (delta > 0.0f)
        {
            _animation.Update(delta);
        }
    }

    protected override void OnRender(UiRenderContext context)
    {
        base.OnRender(context);

        if (Bounds.Width <= 0.0f || Bounds.Height <= 0.0f)
        {
            return;
        }

        EnsureAnimation();

        context.PushClip(Bounds);

        try
        {
            context.DrawText(
                "Sprite Animation Preview",
                new Vector2(Bounds.X + 8.0f, Bounds.Y + 8.0f),
                13.0f,
                new UiColor(220, 225, 235, 255));

            if (_animation is null)
            {
                context.DrawText(
                    _error ?? "Preview unavailable.",
                    new Vector2(Bounds.X + 8.0f, Bounds.Y + 34.0f),
                    12.0f,
                    new UiColor(225, 160, 110, 255));

                return;
            }

            var sprite = _animation.Current;

            if (_textureManager?.Exists(sprite.Texture) != true)
            {
                context.DrawText(
                    "Frame texture is unavailable.",
                    new Vector2(Bounds.X + 8.0f, Bounds.Y + 34.0f),
                    12.0f,
                    new UiColor(225, 160, 110, 255));

                return;
            }

            var availableWidth =
                MathF.Max(1.0f, Bounds.Width - 24.0f);

            var availableHeight =
                MathF.Max(1.0f, Bounds.Height - 66.0f);

            var scale = MathF.Min(
                availableWidth / sprite.Size.X,
                availableHeight / sprite.Size.Y);

            var width = sprite.Size.X * scale;
            var height = sprite.Size.Y * scale;

            var destination = new UiRect(
                Bounds.X + (Bounds.Width - width) * 0.5f,
                Bounds.Y + 36.0f +
                    (availableHeight - height) * 0.5f,
                width,
                height);

            context.DrawTexture(
                sprite.Texture,
                destination,
                sprite.UV);

            context.DrawText(
                $"Frame {_animation.CurrentFrame + 1}/{_animation.FrameCount}",
                new Vector2(Bounds.X + 8.0f, Bounds.Bottom - 20.0f),
                12.0f,
                new UiColor(190, 200, 215, 255));
        }
        finally
        {
            context.PopClip();
        }
    }

    private void EnsureAnimation()
    {
        var definition = _document.Definition;

        if (ReferenceEquals(definition, _loadedDefinition))
        {
            return;
        }

        ReleaseTextures();

        _loadedDefinition = definition;
        _animation = null;
        _error = null;

        if (definition.Frames.Count == 0)
        {
            _error = "Add at least one frame to preview the animation.";
            return;
        }

        if (_textureManager is null)
        {
            _error = "Texture preview is unavailable.";
            return;
        }

        var assetsRoot =
            _editor.ActiveGameProject?.AssetsDirectoryPath;

        if (string.IsNullOrWhiteSpace(assetsRoot))
        {
            _error = "Open a game project to preview textures.";
            return;
        }

        try
        {
            var root = Path.GetFullPath(assetsRoot);
            var sprites = new List<Sprite>(definition.Frames.Count);

            foreach (var frame in definition.Frames)
            {
                if (!_textures.TryGetValue(
                        frame.TexturePath,
                        out var texture))
                {
                    var texturePath =
                        EditorProjectAssetPathResolver.Resolve(
                            root,
                            frame.TexturePath,
                            "Frame texture");

                    var imageData = _textureLoader.Load(
                        File.ReadAllBytes(texturePath));

                    texture = _textureManager.Create(imageData);

                    if (!texture.IsValid ||
                        !_textureManager.Exists(texture))
                    {
                        throw new InvalidOperationException(
                            $"Could not load texture: {frame.TexturePath}");
                    }

                    _textures.Add(frame.TexturePath, texture);
                }

                var description =
                    _textureManager.GetDescription(texture);

                sprites.Add(
                    new Sprite(
                        texture,
                        new Rectangle(0.0f, 0.0f, 1.0f, 1.0f),
                        new Vector2(
                            description.Width,
                            description.Height)));
            }

            _animation = new SpriteAnimation(
                sprites,
                definition.FrameDurationSeconds,
                definition.Loop);
        }
        catch (Exception exception)
        {
            ReleaseTextures();
            _animation = null;
            _error = exception.Message;
        }
    }
    public void ReleaseResources()
    {
        ReleaseTextures();
        _loadedDefinition = null;
        _animation = null;
    }

    private void ReleaseTextures()
    {
        if (_textureManager is not null)
        {
            foreach (var texture in _textures.Values)
            {
                if (_textureManager.Exists(texture))
                {
                    _textureManager.Destroy(texture);
                }
            }
        }

        _textures.Clear();
    }
}