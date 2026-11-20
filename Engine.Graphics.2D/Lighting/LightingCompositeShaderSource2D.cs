using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Lighting;

public static class LightingCompositeShaderSource2D
{
    public const string SceneColor = "uSceneColor";
    public const string DirectLighting = "uDirectLighting";
    public const string AmbientLighting = "uAmbientLighting";
    public const string IndirectLighting = "uIndirectLighting";
    public const string Emission = "uEmission";

    private const string GlslFragment = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uSceneColor;
        uniform sampler2D uDirectLighting;
        uniform sampler2D uAmbientLighting;
        uniform sampler2D uIndirectLighting;
        uniform sampler2D uEmission;

        out vec4 FragColor;

        void main()
        {
            vec4 scene = texture(uSceneColor, vTexCoord);

            vec3 direct = texture(
                uDirectLighting,
                vTexCoord).rgb;

            vec3 ambient = texture(
                uAmbientLighting,
                vTexCoord).rgb;

            vec3 indirect = texture(
                uIndirectLighting,
                vTexCoord).rgb;

            vec3 emission = texture(
                uEmission,
                vTexCoord).rgb;

            vec3 lightedColor =
                scene.rgb * ambient +
                direct +
                indirect +
                emission;

            FragColor = vec4(lightedColor, scene.a);
        }
        """;

    private const string HlslFragment = """
        Texture2D<float4> uSceneColor : register(t0);
        Texture2D<float4> uDirectLighting : register(t1);
        Texture2D<float4> uAmbientLighting : register(t2);
        Texture2D<float4> uIndirectLighting : register(t3);
        Texture2D<float4> uEmission : register(t4);

        SamplerState uSampler : register(s0);

        struct PixelInput
        {
            float4 Position : SV_POSITION;
            float2 TexCoord : TEXCOORD0;
        };

        float4 PSMain(PixelInput input) : SV_TARGET
        {
            float4 scene = uSceneColor.Sample(
                uSampler,
                input.TexCoord);

            float3 direct = uDirectLighting.Sample(
                uSampler,
                input.TexCoord).rgb;

            float3 ambient = uAmbientLighting.Sample(
                uSampler,
                input.TexCoord).rgb;

            float3 indirect = uIndirectLighting.Sample(
                uSampler,
                input.TexCoord).rgb;

            float3 emission = uEmission.Sample(
                uSampler,
                input.TexCoord).rgb;

            float3 lightedColor =
                scene.rgb * ambient +
                direct +
                indirect +
                emission;

            return float4(lightedColor, scene.a);
        }
        """;

    public static ShaderDescription CreateDescription()
    {
        return new ShaderDescription(
            new ShaderVariant(
                ShaderLanguage.Glsl,
                FullscreenShaderSource.GlslVertex,
                GlslFragment),

            new ShaderVariant(
                ShaderLanguage.Hlsl,
                FullscreenShaderSource.HlslVertex,
                HlslFragment,
                "VSMain",
                "PSMain"));
    }
}