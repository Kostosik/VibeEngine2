using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Describes the GPU resources used by 2D lighting channels.
/// Resources are owned and managed by the Render Graph.
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

        var descriptor = new RenderGraphResourceDescriptor(
            width,
            height,
            TextureFormat.Rgba16Float)
        {
            ColorSpace = TextureColorSpace.Linear,
            HasDepthAttachment = false
        };

        var direct = builder.CreateTexture(
            $"{namePrefix}.DirectLighting",
            descriptor);

        var ambient = builder.CreateTexture(
            $"{namePrefix}.AmbientLighting",
            descriptor);

        var indirect = builder.CreateTexture(
            $"{namePrefix}.IndirectLighting",
            descriptor);

        var emission = builder.CreateTexture(
            $"{namePrefix}.Emission",
            descriptor);

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

    /// <summary>
    /// Adds passes that initialize all lighting buffers to black.
    /// The first pass depends on the supplied previous pipeline pass.
    /// </summary>
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