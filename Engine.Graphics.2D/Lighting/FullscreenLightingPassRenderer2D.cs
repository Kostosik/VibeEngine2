using System.Globalization;
using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Lighting;

public sealed class FullscreenLightingPassRenderer2D :
    ILightingPassRenderer2D
{
    private readonly ShaderHandle _pointLightingShader;
    private readonly ShaderHandle _ambientLightingShader;

    public FullscreenLightingPassRenderer2D(
        ShaderHandle pointLightingShader,
        ShaderHandle ambientLightingShader)
    {
        if (!pointLightingShader.IsValid)
        {
            throw new ArgumentException(
                "A valid point-lighting shader is required.",
                nameof(pointLightingShader));
        }

        if (!ambientLightingShader.IsValid)
        {
            throw new ArgumentException(
                "A valid ambient-lighting shader is required.",
                nameof(ambientLightingShader));
        }

        _pointLightingShader = pointLightingShader;
        _ambientLightingShader = ambientLightingShader;
    }

    public void Render(
        in LightingPassContext2D context,
        RenderGraphContext renderGraphContext)
    {
        ArgumentNullException.ThrowIfNull(renderGraphContext);

        switch (context.Channel)
        {
            case LightingChannel2D.Direct:
                RenderPointLights(
                    in context,
                    renderGraphContext);
                break;

            case LightingChannel2D.Ambient:
                RenderAmbient(
                    in context,
                    renderGraphContext);
                break;

            case LightingChannel2D.Indirect:
            case LightingChannel2D.Emission:
                EnsureChannelIsEmpty(context);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Channel,
                    "Unsupported lighting channel.");
        }
    }

    private void RenderPointLights(
        in LightingPassContext2D context,
        RenderGraphContext renderGraphContext)
    {
        var lights = new List<Light2D>(
            context.Contributions.Count);

        foreach (var contribution in context.Contributions)
        {
            if (contribution is not Light2D light)
            {
                throw new NotSupportedException(
                    $"Unsupported direct-light contribution: " +
                    $"{contribution.GetType().Name}.");
            }

            if (light.IsEnabled)
            {
                lights.Add(light);
            }
        }

        if (lights.Count == 0)
            return;

        if (lights.Count > LightingShaderSource2D.MaxPointLights)
        {
            throw new InvalidOperationException(
                $"The 2D lighting shader supports at most " +
                $"{LightingShaderSource2D.MaxPointLights} point lights per frame. " +
                $"Received: {lights.Count}.");
        }

        var lightingContext = context.LightingContext;

        var parameters = new PointLightGpuData2D[lights.Count];

        for (var i = 0; i < lights.Count; i++)
        {
            var light = lights[i];

            parameters[i] = PointLightGpuData2D.From(
                in light,
                in lightingContext);
        }

        var pass = new FullscreenPassDescription(
            _pointLightingShader,
            writer =>
            {
                writer.SetInt32(
                    "uLightCount",
                    parameters.Length);

                for (var i = 0; i < parameters.Length; i++)
                {
                    var index = i.ToString(
                        CultureInfo.InvariantCulture);

                    var light = parameters[i];

                    writer.SetVector2(
                        $"uLightPositions[{index}]",
                        light.ScreenPosition);

                    writer.SetVector2(
                        $"uLightRadii[{index}]",
                        light.ScreenRadius);

                    writer.SetVector3(
                        $"uLightColors[{index}]",
                        light.Color);

                    writer.SetFloat(
                        $"uLightIntensities[{index}]",
                        light.Intensity);
                }
            },
            [
                new FullscreenPassTextureInput(
                    LightingShaderSource2D.SceneColor,
                    context.SceneColor)
            ]);

        renderGraphContext.ExecuteFullscreen(pass);
    }

    private void RenderAmbient(
        in LightingPassContext2D context,
        RenderGraphContext renderGraphContext)
    {
        var ambientColor = Vector3.Zero;

        foreach (var contribution in context.Contributions)
        {
            if (contribution is not AmbientLight2D light)
            {
                throw new NotSupportedException(
                    $"Unsupported ambient-light contribution: " +
                    $"{contribution.GetType().Name}.");
            }

            if (!light.IsEnabled)
                continue;

            ambientColor += light.Color * light.Intensity;
        }

        if (!float.IsFinite(ambientColor.X) ||
            !float.IsFinite(ambientColor.Y) ||
            !float.IsFinite(ambientColor.Z))
        {
            throw new InvalidOperationException(
                "Accumulated ambient-light intensity is not finite.");
        }

        if (ambientColor == Vector3.Zero)
            return;

        var pass = new FullscreenPassDescription(
            _ambientLightingShader,
            writer =>
            {
                writer.SetVector3(
                    "uAmbientColor",
                    ambientColor);
            },
            [
                new FullscreenPassTextureInput(
                    LightingShaderSource2D.SceneColor,
                    context.SceneColor)
            ]);

        renderGraphContext.ExecuteFullscreen(pass);
    }

    private static void EnsureChannelIsEmpty(
        in LightingPassContext2D context)
    {
        if (context.Contributions.Count > 0)
        {
            throw new NotSupportedException(
                $"Channel '{context.Channel}' has contributions, " +
                "but its GPU renderer has not been implemented yet.");
        }
    }
}