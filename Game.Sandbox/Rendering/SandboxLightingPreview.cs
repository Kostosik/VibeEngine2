using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Lighting;

namespace Game.Sandbox.Rendering;

/// <summary>
/// Temporary integration probe for the 2D lighting pipeline.
/// </summary>
internal sealed class SandboxLightingPreview : IDisposable
{
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

        var cameraPosition = _camera.Position;

        // The radius occupies approximately 42% of the viewport height.
        var radius =
            _camera.ViewportSize.Y / _camera.Zoom * 0.42f;

        var offset = radius * 0.28f;

        _pointLights.SetLights(
        [
            new Light2D(
                new Vector2(
                    cameraPosition.X - offset,
                    cameraPosition.Y),
                new Vector3(1.0f, 0.48f, 0.16f),
                radius,
                1.0f),

            new Light2D(
                new Vector2(
                    cameraPosition.X + offset,
                    cameraPosition.Y),
                new Vector3(0.16f, 0.48f, 1.0f),
                radius,
                1.0f)
        ]);
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