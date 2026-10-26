
using Engine.Core.Assets;
using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Particles;
using Engine.Particles.Emission;
using Engine.Particles.Particles;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.Editor.UI.Workspace;

public sealed class ParticleEffectPreviewView : UiPanel
{
    private const float PixelsPerWorldUnit = 24.0f;
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 64.0f;
    private const float ZoomStep = 1.15f;

    private float _zoom = 1.0f;
    private Vector2 _panOffset;

    private bool _isPanning;
    private Vector2 _lastPanPosition;

    private readonly ParticleEffectDocument _document;
    private readonly EditorContext? _editor;
    private readonly ITextureManager? _textureManager;
    private readonly ImageTextureLoader _textureLoader = new();

    private ParticleEffectDefinition? _simulationDefinition;
    private ParticleEffectSimulation? _simulation;

    private TextureHandle _texture;
    private string? _textureKey;
    private string? _textureError;

    public ParticleEffectPreviewView(
        ParticleEffectDocument document,
        EditorContext? editor,
        ITextureManager? textureManager)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _editor = editor;
        _textureManager = textureManager;

        Height = 150.0f;
        HorizontalAlignment =
            Engine.UI.Layout.UiHorizontalAlignment.Stretch;

        Background = new UiColor(15, 17, 21, 255);
    }

    protected override void OnPointerWheel(
    UiPointerEvent pointer)
    {
        base.OnPointerWheel(pointer);

        if (MathF.Abs(pointer.ScrollDelta) < 0.0001f)
        {
            return;
        }

        var localPosition = new Vector2(
            pointer.Position.X - Bounds.X,
            pointer.Position.Y - Bounds.Y);

        if (!ContainsLocalPosition(localPosition))
        {
            return;
        }

        var newZoom = Math.Clamp(
            _zoom * MathF.Pow(ZoomStep, pointer.ScrollDelta),
            MinZoom,
            MaxZoom);

        if (MathF.Abs(newZoom - _zoom) < 0.000001f)
        {
            return;
        }

        // Масштабируем относительно курсора, а не центра превью.
        var originX = Bounds.Width * 0.5f + _panOffset.X;
        var originY = Bounds.Height * 0.68f + _panOffset.Y;

        var factor = newZoom / _zoom;

        _panOffset = new Vector2(
            _panOffset.X +
            (localPosition.X - originX) * (1.0f - factor),

            _panOffset.Y +
            (localPosition.Y - originY) * (1.0f - factor));

        _zoom = newZoom;

        pointer.Handled = true;
    }

    protected override void OnUpdate(double deltaSeconds)
    {
        EnsureSimulation();

        if (_simulation is null)
        {
            return;
        }

        // Не допускаем огромного скачка после паузы окна.
        var delta = (float)Math.Clamp(
            deltaSeconds,
            0.0,
            0.05);

        if (delta <= 0.0f)
        {
            return;
        }

        _simulation.Update(
            delta,
            Vector2.Zero);
    }

    protected override void OnRender(UiRenderContext context)
    {
        base.OnRender(context);

        if (Bounds.Width <= 0.0f || Bounds.Height <= 0.0f)
        {
            return;
        }

        EnsureSimulation();

        var hasTexture = EnsureTextureLoaded();

        context.PushClip(Bounds);

        try
        {
            var origin = new Vector2(
                Bounds.X + Bounds.Width * 0.5f + _panOffset.X,
                Bounds.Y + Bounds.Height * 0.68f + _panOffset.Y);

            var pixelsPerWorldUnit = PixelsPerWorldUnit * _zoom;

            var guideColor = new UiColor(55, 60, 68, 255);

            context.DrawRectangle(
                new Engine.UI.Layout.UiRect(
                    Bounds.X,
                    origin.Y,
                    Bounds.Width,
                    1.0f),
                guideColor);

            context.DrawRectangle(
                new Engine.UI.Layout.UiRect(
                    origin.X,
                    Bounds.Y,
                    1.0f,
                    Bounds.Height),
                guideColor);

            if (_simulation is not null)
            {
                foreach (var particle in _simulation.ParticleSystem.Particles)
                {
                    if (!float.IsFinite(particle.Position.X) ||
                        !float.IsFinite(particle.Position.Y) ||
                        !float.IsFinite(particle.Size) ||
                        particle.Size <= 0.0f)
                    {
                        continue;
                    }

                    var size = MathF.Max(
                        2.0f,
                        particle.Size * pixelsPerWorldUnit);

                    var centerX =
                        origin.X +
                        particle.Position.X * pixelsPerWorldUnit;

                    var centerY =
                        origin.Y -
                        particle.Position.Y * pixelsPerWorldUnit;

                    var rect = new Engine.UI.Layout.UiRect(
                        centerX - size * 0.5f,
                        centerY - size * 0.5f,
                        size,
                        size);

                    if (hasTexture)
                    {
                        context.DrawTexture(
                            _texture,
                            rect,
                            new Rectangle(0.0f, 0.0f, 1.0f, 1.0f));
                    }
                    else
                    {
                        // Позволяет видеть симуляцию даже тогда,
                        // когда текстура отсутствует.
                        context.DrawRectangle(
                            rect,
                            new UiColor(235, 155, 75, 255));
                    }
                }
            }

            var particleCount =
                _simulation?.ParticleSystem.Count ?? 0;

            var capacity =
                _simulation?.ParticleSystem.Capacity ?? 0;

            context.DrawText(
                $"Preview • Particles: {particleCount}/{capacity} • Zoom: {_zoom * 100.0f:0}%",
                new Vector2(Bounds.X + 8.0f, Bounds.Y + 6.0f),
                12.0f,
                new UiColor(220, 225, 235, 255));

            if (_textureError is not null)
            {
                context.DrawText(
                    _textureError,
                    new Vector2(Bounds.X + 8.0f, Bounds.Bottom - 19.0f),
                    10.0f,
                    new UiColor(225, 160, 110, 255));
            }
        }
        finally
        {
            context.PopClip();
        }
    }

    protected override void OnPointerDown(
    UiPointerEvent pointer)
    {
        if (pointer.Button != Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        var localPosition = new Vector2(
            pointer.Position.X - Bounds.X,
            pointer.Position.Y - Bounds.Y);

        if (!ContainsLocalPosition(localPosition))
        {
            return;
        }

        _isPanning = true;
        _lastPanPosition = pointer.Position;

        pointer.RequestCapture();
        pointer.Handled = true;
    }

    protected override void OnPointerMove(
        UiPointerEvent pointer)
    {
        base.OnPointerMove(pointer);

        if (!_isPanning ||
            pointer.Button != Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        var delta = new Vector2(
            pointer.Position.X - _lastPanPosition.X,
            pointer.Position.Y - _lastPanPosition.Y);

        _lastPanPosition = pointer.Position;

        _panOffset = new Vector2(
            _panOffset.X + delta.X,
            _panOffset.Y + delta.Y);

        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        base.OnPointerUp(pointer);

        if (!_isPanning ||
            pointer.Button != Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        _isPanning = false;
        pointer.Handled = true;
    }

    private void EnsureSimulation()
    {
        var definition = _document.Definition;

        if (ReferenceEquals(
                definition,
                _simulationDefinition))
        {
            return;
        }

        _simulationDefinition = definition;

        // Фиксированный seed делает превью воспроизводимым.
        _simulation = new ParticleEffectSimulation(
            definition,
            seed: 1UL);
    }

    private static ParticleFactoryDelegate CreateParticleFactory(
        ParticleEffectDefinition definition)
    {
        return (ref DeterministicRandom random) =>
        {
            var lifetime = random.NextFloat(
                definition.LifetimeSeconds.Min,
                definition.LifetimeSeconds.Max);

            var size = random.NextFloat(
                definition.Size.Min,
                definition.Size.Max);

            var velocityX = random.NextFloat(
                definition.Velocity.Min.X,
                definition.Velocity.Max.X);

            var velocityY = random.NextFloat(
                definition.Velocity.Min.Y,
                definition.Velocity.Max.Y);

            return new Particle(
                Vector3.Zero,
                new Vector3(velocityX, velocityY, 0.0f),
                lifetime,
                size);
        };
    }

    private bool EnsureTextureLoaded()
    {
        var assetsRoot =
            _editor?.ActiveGameProject?.AssetsDirectoryPath;

        if (_textureManager is null ||
            string.IsNullOrWhiteSpace(assetsRoot))
        {
            ReleaseTexture();

            _textureKey = null;
            _textureError = _textureManager is null
                ? "Texture preview is unavailable."
                : "Open a game project to load the texture.";

            return false;
        }

        var key =
            Path.GetFullPath(assetsRoot) +
            "|" +
            _document.Definition.TexturePath;

        if (string.Equals(
                key,
                _textureKey,
                StringComparison.Ordinal))
        {
            return _texture.IsValid &&
                   _textureManager.Exists(_texture);
        }

        ReleaseTexture();

        _textureKey = key;
        _textureError = null;

        try
        {
            var texturePath = ResolveTexturePath(
                assetsRoot,
                _document.Definition.TexturePath);

            var imageData = _textureLoader.Load(
                File.ReadAllBytes(texturePath));

            _texture = _textureManager.Create(imageData);

            if (!_texture.IsValid ||
                !_textureManager.Exists(_texture))
            {
                throw new InvalidOperationException(
                    "The graphics backend could not create the preview texture.");
            }

            return true;
        }
        catch
        {
            _textureError =
                $"Texture unavailable: {_document.Definition.TexturePath}";

            ReleaseTexture();
            return false;
        }
    }

    private static string ResolveTexturePath(
        string assetsRoot,
        string texturePath)
    {
        var fullRoot = Path.GetFullPath(assetsRoot);

        if (Path.IsPathRooted(texturePath))
        {
            throw new InvalidDataException(
                "Particle textures must use paths relative to Assets.");
        }

        var normalizedPath = new AssetPath(texturePath).Value;

        var fullPath = Path.GetFullPath(
            Path.Combine(fullRoot, normalizedPath));

        var relativePath = Path.GetRelativePath(
            fullRoot,
            fullPath);

        if (Path.IsPathRooted(relativePath) ||
            relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Particle texture path points outside the Assets directory.");
        }

        return fullPath;
    }

    public void ReleaseResources()
    {
        ReleaseTexture();
        _textureKey = null;
    }

    private void ReleaseTexture()
    {
        if (_texture.IsValid &&
            _textureManager?.Exists(_texture) == true)
        {
            _textureManager.Destroy(_texture);
        }

        _texture = default;
    }

    private bool ContainsLocalPosition(Vector2 position)
    {
        return position.X >= 0.0f &&
               position.Y >= 0.0f &&
               position.X <= Bounds.Width &&
               position.Y <= Bounds.Height;
    }

}
