using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;

namespace Game.Sandbox.Rendering;

internal sealed class MultiTextureBindingProbe :
    IRenderGraphExtension,
    IDisposable
{
    private const string ExtensionName =
        "MultiTextureBindingProbe";

    private const string PipelinePassName = "World";

    private readonly IShaderManager _shaders;

    private readonly ShaderHandle _maskShader;
    private readonly ShaderHandle _compositeShader;

    private readonly FullscreenPassDescription _maskPass;

    private bool _disposed;

    private static readonly TextureSamplerDescription SceneSampler = new(
        TextureFilter.Linear,
        TextureFilter.Linear,
        TextureWrapMode.ClampToEdge,
        TextureWrapMode.ClampToEdge);

    private static readonly TextureSamplerDescription MaskSampler = new(
        TextureFilter.Nearest,
        TextureFilter.Nearest,
        TextureWrapMode.ClampToEdge,
        TextureWrapMode.ClampToEdge);

    public string Name => ExtensionName;

    public string AfterPipelinePassName => PipelinePassName;

    public int Order => 0;

    public MultiTextureBindingProbe(IShaderManager shaders)
    {
        ArgumentNullException.ThrowIfNull(shaders);

        _shaders = shaders;

        _maskShader = shaders.Create(
            new ShaderDescription(
                FullscreenShaderSource.Vertex,
                MaskFragmentShader));

        try
        {
            _compositeShader = shaders.Create(
                new ShaderDescription(
                    FullscreenShaderSource.Vertex,
                    CompositeFragmentShader));
        }
        catch
        {
            shaders.Destroy(_maskShader);
            throw;
        }

        _maskPass = new FullscreenPassDescription(
            _maskShader);
    }

    public RenderGraphExtensionResult Build(
        RenderGraphBuilder builder,
        in RenderGraphExtensionContext context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var width = context.Width;
        var height = context.Height;
        var sceneColor = context.SceneColor;
        var previousPass = context.PreviousPass;

        var descriptor = new RenderGraphResourceDescriptor(
            context.Width,
            context.Height);

        var mask = builder.CreateTexture(
            "MultiTextureProbe.Mask",
            descriptor);

        var output = builder.CreateTexture(
            "MultiTextureProbe.Output",
            descriptor);

        var maskPass = builder.AddPass(
            "MultiTextureProbe.BuildMask",
            pass => pass
                .Read(sceneColor)
                .WriteColor(mask)
                .DependsOn(previousPass)
                .SetRenderState(RenderState.Default2D)
                .SetExecuteQueuedCommands(false)
                .SetExecute(passContext =>
                    passContext.ExecuteFullscreen(
                        _maskPass,
                        sceneColor)));

        var compositeDescription =
            new FullscreenPassDescription(
                _compositeShader,
                inputs:
                [
                    new FullscreenPassTextureInput(
                        "uSceneColor",
                        context.SceneColor,
                        SceneSampler),

                    new FullscreenPassTextureInput(
                        "uMask",
                        mask,
                        MaskSampler)
                ]);

        var compositePass = builder.AddPass(
            "MultiTextureProbe.Composite",
            pass => pass
                .Read(sceneColor)
                .Read(mask)
                .WriteColor(output)
                .DependsOn(maskPass)
                .SetPostProcessSource()
                .SetRenderState(RenderState.Default2D)
                .SetExecuteQueuedCommands(false)
                .SetExecute(passContext =>
                    passContext.ExecuteFullscreen(
                        compositeDescription)));

        return new RenderGraphExtensionResult(
            compositePass,
            output);
    }

    private const string MaskFragmentShader = """
    #version 330 core

    in vec2 vTexCoord;

    uniform sampler2D uSource;

    out vec4 FragColor;

    void main()
    {
        vec2 uv = vec2(
            vTexCoord.x,
            1.0 - vTexCoord.y);

        vec3 scene = texture(uSource, uv).rgb;

        float luminance = dot(
            scene,
            vec3(0.2126, 0.7152, 0.0722));

        float horizontal = smoothstep(
            0.35,
            0.65,
            vTexCoord.x);

        float mask = clamp(
            horizontal * 0.7 +
            luminance * 0.3,
            0.0,
            1.0);

        FragColor = vec4(
            mask,
            mask,
            mask,
            1.0);
    }
    """;

    private const string CompositeFragmentShader = """
    #version 330 core

    in vec2 vTexCoord;

    uniform sampler2D uSceneColor;
    uniform sampler2D uMask;

    out vec4 FragColor;

    void main()
    {
        vec2 uv = vec2(
            vTexCoord.x,
            1.0 - vTexCoord.y);

        vec3 scene = texture(
            uSceneColor,
            uv).rgb;

        float mask = texture(
            uMask,
            uv).r;

        vec3 tinted = scene *
            vec3(0.35, 0.8, 1.0);

        FragColor = vec4(
            mix(scene, tinted, mask * 0.85),
            1.0);
    }
    """;

    public void Dispose()
    {
        if (_disposed)
            return;

        var errors = new List<Exception>();

        try
        {
            _shaders.Destroy(_compositeShader);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        try
        {
            _shaders.Destroy(_maskShader);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "Multi-texture probe shaders could not be released.",
                errors);
        }
    }
}