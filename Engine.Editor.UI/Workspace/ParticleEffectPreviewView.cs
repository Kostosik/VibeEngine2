
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

namespace Engine.Editor.UI.Workspace;

public sealed class ParticleEffectPreviewView : UiPanel
{
    private const float PixelsPerWorldUnit = 24.0f;

    private readonly ParticleEffectDocument _document;
    private readonly EditorContext? _editor;
    private readonly ITextureManager? _textureManager;
    private readonly ImageTextureLoader _textureLoader = new();

    private ParticleEffectDefinition? _simulationDefinition;
    private ParticleSystem? _particles;
    private ParticleEmitterRuntime? _emitterRuntime;
    private ParticleFactoryDelegate? _particleFactory;

    private DeterministicRandom _random = new(1UL);

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

    protected override void OnUpdate(double deltaSeconds)
    {
        EnsureSimulation();

        if (_particles is null ||
            _emitterRuntime is null ||
            _particleFactory is null)
        {
            return;
        }

        // Не допускаем огромного скачка симуляции после паузы окна.
        var delta = (float)Math.Clamp(deltaSeconds, 0.0, 0.05);

        if (delta <= 0.0f)
        {
            return;
        }

        _particles.Update(delta);

        _emitterRuntime.Update(
            _particles,
            delta,
            ref _random,
            _particleFactory);
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
                Bounds.X + Bounds.Width * 0.5f,
                Bounds.Y + Bounds.Height * 0.68f);

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

            if (_particles is not null)
            {
                foreach (var particle in _particles.Particles)
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
                        particle.Size * PixelsPerWorldUnit);

                    var centerX =
                        origin.X +
                        particle.Position.X * PixelsPerWorldUnit;

                    var centerY =
                        origin.Y -
                        particle.Position.Y * PixelsPerWorldUnit;

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

            var particleCount = _particles?.Count ?? 0;
            var capacity = _particles?.Capacity ?? 0;

            context.DrawText(
                $"Preview • Particles: {particleCount}/{capacity}",
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

        _particles = new ParticleSystem(
            definition.MaxParticles);

        _emitterRuntime = new ParticleEmitterRuntime(
            new ParticleEmitter(
                definition.EmissionRatePerSecond));

        _particleFactory = CreateParticleFactory(definition);

        // Повторяемая последовательность полезна для отладки эффекта.
        _random = new DeterministicRandom(1UL);
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
}
