
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Describes GPU resources used by 2D lighting channels.
/// All resources are owned by the Render Graph.
///
/// Indirect Lighting and Emission temporarily share
/// a 1x1 neutral black texture until their renderers
/// are implemented.
/// </summary>
public sealed class LightingBufferSet2D
{
    private readonly string _namePrefix;

    public RenderGraphResourceHandle DirectLighting { get; }

    public RenderGraphResourceHandle AmbientLighting { get; }

    public RenderGraphResourceHandle IndirectLighting { get; }

    public RenderGraphResourceHandle Emission { get; }

    private LightingBufferSet2D(
        string namePrefix,
        RenderGraphResourceHandle directLighting,
        RenderGraphResourceHandle ambientLighting,
        RenderGraphResourceHandle indirectLighting,
        RenderGraphResourceHandle emission)
    {
        _namePrefix = namePrefix;

        DirectLighting = directLighting;
        AmbientLighting = ambientLighting;
        IndirectLighting = indirectLighting;
        Emission = emission;
    }

    public static LightingBufferSet2D Create(
        RenderGraphBuilder builder,
        int width,
        int height,
        string namePrefix = "Lighting2D")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentException.ThrowIfNullOrWhiteSpace(namePrefix);

        var fullSizeDescriptor =
            new RenderGraphResourceDescriptor(
                width,
                height,
                TextureFormat.Rgba16Float)
            {
                ColorSpace = TextureColorSpace.Linear,
                HasDepthAttachment = false
            };

        var neutralDescriptor =
            new RenderGraphResourceDescriptor(
                1,
                1,
                TextureFormat.Rgba16Float)
            {
                ColorSpace = TextureColorSpace.Linear,
                HasDepthAttachment = false
            };

        var direct = builder.CreateTexture(
            $"{namePrefix}.DirectLighting",
            fullSizeDescriptor);

        var ambient = builder.CreateTexture(
            $"{namePrefix}.AmbientLighting",
            fullSizeDescriptor);

        var indirect = builder.CreateTexture(
            $"{namePrefix}.IndirectLighting",
            neutralDescriptor);

        var emission = builder.CreateTexture(
            $"{namePrefix}.Emission",
            neutralDescriptor);

        return new LightingBufferSet2D(
            namePrefix,
            direct,
            ambient,
            indirect,
            emission);
    }

    public RenderGraphResourceHandle GetResource(
        LightingChannel2D channel)
    {
        return channel switch
        {
            LightingChannel2D.Direct => DirectLighting,
            LightingChannel2D.Ambient => AmbientLighting,
            LightingChannel2D.Indirect => IndirectLighting,
            LightingChannel2D.Emission => Emission,

            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "Exactly one supported lighting channel is required.")
        };
    }

    public RenderGraphPassHandle AddClearPasses(
        RenderGraphBuilder builder,
        RenderGraphPassHandle previousPass)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!previousPass.IsValid)
        {
            throw new ArgumentException(
                "A valid previous render-graph pass is required.",
                nameof(previousPass));
        }

        var current = AddClearPass(
            builder,
            $"{_namePrefix}.Clear.DirectLighting",
            DirectLighting,
            previousPass);

        current = AddClearPass(
            builder,
            $"{_namePrefix}.Clear.AmbientLighting",
            AmbientLighting,
            current);

        current = AddClearPass(
            builder,
            $"{_namePrefix}.Clear.IndirectLighting",
            IndirectLighting,
            current);

        current = AddClearPass(
            builder,
            $"{_namePrefix}.Clear.Emission",
            Emission,
            current);

        return current;
    }

    private static RenderGraphPassHandle AddClearPass(
        RenderGraphBuilder builder,
        string passName,
        RenderGraphResourceHandle resource,
        RenderGraphPassHandle dependency)
    {
        return builder.AddPass(
            passName,
            pass =>
            {
                pass.WriteColor(
                    resource,
                    RenderGraphLoadAction.Clear);

                pass.SetExecuteQueuedCommands(false);
                pass.DependsOn(dependency);
            });
    }
}
