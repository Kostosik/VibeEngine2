using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Describes GPU resources used by 2D lighting channels.
/// All resources are owned by the Render Graph.
/// </summary>
public sealed class LightingBufferSet2D
{
    private readonly string _namePrefix;

    public RenderGraphResourceHandle DirectLighting { get; }

    public RenderGraphResourceHandle AmbientLighting { get; }

    public RenderGraphResourceHandle IndirectLighting { get; }

    public RenderGraphResourceHandle Emission { get; }

    public RenderGraphResourceHandle NormalBuffer { get; }

    private LightingBufferSet2D(
        string namePrefix,
        RenderGraphResourceHandle directLighting,
        RenderGraphResourceHandle ambientLighting,
        RenderGraphResourceHandle indirectLighting,
        RenderGraphResourceHandle emission,
        RenderGraphResourceHandle normalBuffer)
    {
        _namePrefix = namePrefix;

        DirectLighting = directLighting;
        AmbientLighting = ambientLighting;
        IndirectLighting = indirectLighting;
        Emission = emission;
        NormalBuffer = normalBuffer;
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

        var normalBuffer = builder.CreateTexture(
            $"{namePrefix}.NormalBuffer",
            fullSizeDescriptor);

        return new LightingBufferSet2D(
            namePrefix,
            direct,
            ambient,
            indirect,
            emission,
            normalBuffer);
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

        current = AddClearPass(
            builder,
            $"{_namePrefix}.Clear.NormalBuffer",
            NormalBuffer,
            current,
            new Vector4(0.5f, 0.5f, 1f, 1f));

        return current;
    }

    private static RenderGraphPassHandle AddClearPass(
        RenderGraphBuilder builder,
        string passName,
        RenderGraphResourceHandle resource,
        RenderGraphPassHandle dependency,
        Vector4? clearColor = null)
    {
        return builder.AddPass(
            passName,
            pass =>
            {
                pass.WriteColor(
                    resource,
                    RenderGraphLoadAction.Clear);

                if (clearColor.HasValue)
                {
                    pass.SetClearColor(clearColor.Value);
                }

                pass.SetExecuteQueuedCommands(false);
                pass.DependsOn(dependency);
            });
    }
}