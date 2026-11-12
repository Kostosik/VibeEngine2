using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public sealed class GrayscaleEffect : IPostProcessEffect
{
    public const string EffectName = "Grayscale";

    public const string FragmentShaderSource = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uSource;

        out vec4 FragColor;

        void main()
        {
            vec4 color = texture(uSource, vTexCoord);

            float luminance = dot(
                color.rgb,
                vec3(0.299, 0.587, 0.114));

            FragColor = vec4(
                vec3(luminance),
                color.a);
        }
        """;

    public string Name => EffectName;

    public bool Enabled { get; set; } = true;

    public ShaderHandle Shader { get; }

    public GrayscaleEffect(ShaderHandle shader)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentException(
                "A valid shader handle is required.",
                nameof(shader));
        }

        Shader = shader;
    }


    public static ShaderDescription CreateShaderDescription()
    {
        return FullscreenShaderSource.CreateDescription(
            FragmentShaderSource,
            HlslFragmentShaderSource);
    }



    public const string HlslFragmentShaderSource = """
    Texture2D uSource : register(t0);
    SamplerState uSourceSampler : register(s0);

    struct FullscreenPixelInput
    {
        float4 Position : SV_POSITION;
        float2 TexCoord : TEXCOORD0;
    };

    float4 PSMain(FullscreenPixelInput input) : SV_TARGET
    {
        float4 color = uSource.Sample(
            uSourceSampler,
            input.TexCoord);

        float luminance = dot(
            color.rgb,
            float3(0.299f, 0.587f, 0.114f));

        return float4(
            luminance.xxx,
            color.a);
    }
    """;

}