using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Materials;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Lighting;
using Engine.Graphics2D.Sprites;

namespace Game.Sandbox.Rendering;

/// <summary>
/// Temporary integration probe for the 2D lighting pipeline.
/// </summary>
internal sealed class SandboxLightingPreview : IDisposable
{
    private const int ProbeTextureSize = 256;

    private static readonly Vector2 ProbeSpriteSize =
        new(16.0f, 16.0f);

    private readonly SpriteRenderer _spriteRenderer;

    private readonly Sprite _normalMappedSprite = default;

    private readonly TextureHandle _probeAlbedoTexture = default;
    private readonly TextureHandle _probeNormalTexture = default;

    private const string ExtensionName = "Lighting2D";

    private readonly IGraphicsDevice _graphics;
    private readonly Camera2D _camera;
    private readonly PointLightingProvider2D _pointLights;

    private readonly LightingRenderGraphExtension2D _extension;

    private readonly ShaderHandle _pointLightingShader;
    private readonly ShaderHandle _ambientLightingShader;
    private readonly ShaderHandle _compositeShader;

    private bool _disposed;

    public SandboxLightingPreview(
        IGraphicsDevice graphics,
        Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(camera);

        _graphics = graphics;
        _camera = camera;
        _spriteRenderer = new SpriteRenderer(graphics);
        var pointLights = new PointLightingProvider2D();

        var ambient = new AmbientLightingProvider2D(
            new AmbientLight2D(
                new Vector3(0.18f, 0.18f, 0.20f)));

        var providers = new LightingProviderRegistry2D();
        providers.Register(pointLights);
        providers.Register(ambient);

        ShaderHandle pointShader = default;
        ShaderHandle ambientShader = default;
        ShaderHandle compositeShader = default;

        LightingRenderGraphExtension2D? extension = null;
        var extensionRegistered = false;

        try
        {
            var probeTextures = CreateNormalMapProbeTextures(
    graphics.Textures);

            _probeAlbedoTexture = probeTextures.Albedo;
            _probeNormalTexture = probeTextures.Normal;

            _normalMappedSprite = new Sprite(
                new SpriteMaterial(
                    _probeAlbedoTexture,
                    _probeNormalTexture),
                new Rectangle(0.0f, 0.0f, 1.0f, 1.0f),
                ProbeSpriteSize,
                RenderLayers.World);

            pointShader = graphics.Shaders.Create(
                LightingShaderSource2D.CreatePointLightingDescription());

            ambientShader = graphics.Shaders.Create(
                LightingShaderSource2D.CreateAmbientLightingDescription());

            compositeShader = graphics.Shaders.Create(
                LightingCompositeShaderSource2D.CreateDescription());

            var core = new LightingCore2D(providers);

            var renderer = new FullscreenLightingPassRenderer2D(
                pointShader,
                ambientShader);

            var compositeRenderer =
                new FullscreenLightingCompositeRenderer2D(
                    compositeShader);

            extension = new LightingRenderGraphExtension2D(
                core,
                () => _camera,
                renderer,
                compositeRenderer);

            graphics.RenderGraphExtensions.Register(extension);
            extensionRegistered = true;

            _pointLights = pointLights;
            _extension = extension;

            _pointLightingShader = pointShader;
            _ambientLightingShader = ambientShader;
            _compositeShader = compositeShader;
        }
        catch
        {
            if (extensionRegistered && extension is not null)
            {
                try
                {
                    graphics.RenderGraphExtensions.Unregister(
                        extension.Name);
                }
                catch
                {
                    // Preserve the original initialization exception.
                }
            }
            TryDestroyTexture(graphics, _probeNormalTexture);
            TryDestroyTexture(graphics, _probeAlbedoTexture);
            TryDestroyShader(graphics, compositeShader);
            TryDestroyShader(graphics, ambientShader);
            TryDestroyShader(graphics, pointShader);

            throw;
        }
    }

    /// <summary>
    /// Keeps the test lights around the current camera position.
    /// </summary>
    public void Update()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var radius =
            _camera.ViewportSize.Y / _camera.Zoom * 0.42f;

        var offset = radius * 0.28f;
        var center = GetProbeCenter();

        _pointLights.SetLights(
        [
            new Light2D(
            new Vector2(center.X - offset, center.Y),
            new Vector3(1.0f, 0.48f, 0.16f),
            radius,
            1.0f),

        new Light2D(
            new Vector2(center.X + offset, center.Y),
            new Vector3(0.16f, 0.48f, 1.0f),
            radius,
            1.0f)
        ]);
    }

    private static (
    TextureHandle Albedo,
    TextureHandle Normal)
    CreateNormalMapProbeTextures(
        ITextureManager textures)
    {
        ArgumentNullException.ThrowIfNull(textures);

        var albedoPixels = new byte[
            ProbeTextureSize * ProbeTextureSize * 4];

        var normalPixels = new byte[
            ProbeTextureSize * ProbeTextureSize * 4];

        for (var y = 0; y < ProbeTextureSize; y++)
        {
            for (var x = 0; x < ProbeTextureSize; x++)
            {
                var u = (x + 0.5f) / ProbeTextureSize;
                var v = (y + 0.5f) / ProbeTextureSize;

                var nx = u * 2.0f - 1.0f;
                var ny = 1.0f - v * 2.0f;

                var radiusSquared = nx * nx + ny * ny;
                var inside = radiusSquared <= 1.0f;

                var normalX = inside ? nx : 0.0f;
                var normalY = inside ? ny : 0.0f;
                var normalZ = inside
                    ? MathF.Sqrt(MathF.Max(
                        0.0f,
                        1.0f - radiusSquared))
                    : 1.0f;

                var normalLength = MathF.Sqrt(
                    normalX * normalX +
                    normalY * normalY +
                    normalZ * normalZ);

                normalX /= normalLength;
                normalY /= normalLength;
                normalZ /= normalLength;

                var index = (y * ProbeTextureSize + x) * 4;

                // Белый albedo с круглой прозрачной областью.
                albedoPixels[index] = 235;
                albedoPixels[index + 1] = 235;
                albedoPixels[index + 2] = 235;
                albedoPixels[index + 3] = inside
                    ? (byte)255
                    : (byte)0;

                // Tangent-space normal, Linear RGB.
                normalPixels[index] = EncodeNormal(normalX);
                normalPixels[index + 1] = EncodeNormal(normalY);
                normalPixels[index + 2] = EncodeNormal(normalZ);
                normalPixels[index + 3] = 255;
            }
        }

        var sampler = new TextureSamplerDescription(
            TextureFilter.Linear,
            TextureFilter.Linear,
            TextureWrapMode.ClampToEdge,
            TextureWrapMode.ClampToEdge);

        var albedoDescription = new TextureDescription(
            ProbeTextureSize,
            ProbeTextureSize,
            TextureFormat.Rgba8)
        {
            ColorSpace = TextureColorSpace.Srgb,
            Sampler = sampler
        };

        var normalDescription = new TextureDescription(
            ProbeTextureSize,
            ProbeTextureSize,
            TextureFormat.Rgba8)
        {
            ColorSpace = TextureColorSpace.Linear,
            Sampler = sampler
        };

        var albedo = textures.Create(
            new TextureData(
                albedoDescription,
                albedoPixels));

        try
        {
            var normal = textures.Create(
                new TextureData(
                    normalDescription,
                    normalPixels));

            return (albedo, normal);
        }
        catch
        {
            textures.Destroy(albedo);
            throw;
        }
    }

    private static byte EncodeNormal(float value)
    {
        var encoded = (value * 0.5f + 0.5f) * 255.0f;

        return (byte)Math.Clamp(
            (int)MathF.Round(encoded),
            0,
            255);
    }

    public void Render()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _spriteRenderer.DrawWorld(
            _normalMappedSprite,
            GetProbePosition(),
            _camera);
    }

    private Vector2 GetProbePosition()
    {
        return new Vector2(
            _camera.Position.X + 3.0f,
            _camera.Position.Y - ProbeSpriteSize.Y * 0.5f);
    }

    private Vector2 GetProbeCenter()
    {
        var position = GetProbePosition();

        return new Vector2(
            position.X + ProbeSpriteSize.X * 0.5f,
            position.Y + ProbeSpriteSize.Y * 0.5f);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        var failures = new List<Exception>();

        try
        {
            _graphics.RenderGraphExtensions.Unregister(
                _extension.Name);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        TryDestroyTexture(
    _graphics,
    _probeNormalTexture,
    failures);

        TryDestroyTexture(
            _graphics,
            _probeAlbedoTexture,
            failures);

        TryDestroyShader(
            _graphics,
            _compositeShader,
            failures);

        TryDestroyShader(
            _graphics,
            _ambientLightingShader,
            failures);

        TryDestroyShader(
            _graphics,
            _pointLightingShader,
            failures);

        if (failures.Count > 0)
        {
            throw new AggregateException(
                "Lighting preview cleanup failed.",
                failures);
        }
    }

    private static void TryDestroyTexture(
    IGraphicsDevice graphics,
    TextureHandle texture)
    {
        if (!texture.IsValid)
            return;

        try
        {
            if (graphics.Textures.Exists(texture))
                graphics.Textures.Destroy(texture);
        }
        catch
        {
            // Preserve the original initialization exception.
        }
    }

    private static void TryDestroyTexture(
        IGraphicsDevice graphics,
        TextureHandle texture,
        List<Exception> failures)
    {
        if (!texture.IsValid)
            return;

        try
        {
            if (graphics.Textures.Exists(texture))
                graphics.Textures.Destroy(texture);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }

    private static void TryDestroyShader(
        IGraphicsDevice graphics,
        ShaderHandle shader)
    {
        if (!shader.IsValid)
            return;

        try
        {
            if (graphics.Shaders.Exists(shader))
                graphics.Shaders.Destroy(shader);
        }
        catch
        {
            // Preserve the original initialization exception.
        }
    }

    private static void TryDestroyShader(
        IGraphicsDevice graphics,
        ShaderHandle shader,
        List<Exception> failures)
    {
        if (!shader.IsValid)
            return;

        try
        {
            if (graphics.Shaders.Exists(shader))
                graphics.Shaders.Destroy(shader);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }
}