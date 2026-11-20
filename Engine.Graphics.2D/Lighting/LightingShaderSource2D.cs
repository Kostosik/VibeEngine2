using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Lighting;

public static class LightingShaderSource2D
{
    public const string SceneColor = "uSceneColor";
    public const string NormalBuffer = "uNormalBuffer";
    public const int MaxPointLights = 64;

    private const string PointLightGlsl = """
        #version 330 core

        in vec2 vTexCoord;
        uniform sampler2D uSceneColor;
        uniform sampler2D uNormalBuffer;

        uniform int uLightCount;

        uniform vec2 uLightPositions[64];
        uniform vec2 uLightRadii[64];

        uniform vec3 uLightColors[64];
        uniform float uLightIntensities[64];

        out vec4 FragColor;

        void main()
        {
            vec2 pixelPosition = vTexCoord * vec2(textureSize(uSceneColor, 0));

            vec3 directLighting = vec3(0.0);
            vec3 surfaceNormal = normalize(
                texture(uNormalBuffer, vTexCoord).xyz * 2.0 - 1.0);
            for (int i = 0; i < uLightCount; i++)
            {
                vec2 offset =
                    (pixelPosition - uLightPositions[i]) /
                    max(uLightRadii[i], vec2(0.0001));

                float normalizedDistance = length(offset);

                if (normalizedDistance >= 1.0)
                    continue;
                vec3 lightDirection = normalize(vec3(-offset, 1.0));

                float diffuse = max(
                    dot(surfaceNormal, lightDirection),
                    0.0);
                float attenuation =
                    1.0 - normalizedDistance;

                attenuation *= attenuation;
                attenuation *= diffuse;

                directLighting +=
                    uLightColors[i] *
                    uLightIntensities[i] *
                    attenuation;
            }

            vec4 scene = texture(
                uSceneColor,
                vTexCoord);

            FragColor = vec4(
                directLighting,
                scene.a);
        }
        """;

    private const string PointLightHlsl = """
        #define MAX_POINT_LIGHTS 64

        Texture2D<float4> uSceneColor : register(t0);
        Texture2D<float4> uNormalBuffer : register(t1);
        SamplerState uSampler : register(s0);

        cbuffer LightingParameters : register(b0)
        {
            int uLightCount;

            float2 uLightPositions[MAX_POINT_LIGHTS];
            float2 uLightRadii[MAX_POINT_LIGHTS];

            float3 uLightColors[MAX_POINT_LIGHTS];
            float uLightIntensities[MAX_POINT_LIGHTS];
        };

        struct PixelInput
        {
            float4 Position : SV_POSITION;
            float2 TexCoord : TEXCOORD0;
        };

        float4 PSMain(PixelInput input) : SV_TARGET
        {
            uint textureWidth;
            uint textureHeight;

            uSceneColor.GetDimensions(
                textureWidth,
                textureHeight);

            float2 pixelPosition = input.TexCoord * float2(
                textureWidth,
                textureHeight);

            float3 directLighting = float3(0.0, 0.0, 0.0);
            float3 surfaceNormal = normalize(
                uNormalBuffer.Sample(
                    uSampler,
                    input.TexCoord).xyz * 2.0 - 1.0);
            for (int i = 0; i < uLightCount; i++)
            {
                float2 offset =
                    (pixelPosition - uLightPositions[i]) /
                    max(uLightRadii[i], float2(0.0001, 0.0001));

                float normalizedDistance = length(offset);

                if (normalizedDistance >= 1.0)
                    continue;
                float3 lightDirection = normalize(
            float3(-offset, 1.0));

        float diffuse = saturate(
            dot(surfaceNormal, lightDirection));
                float attenuation = 1.0 - normalizedDistance;

                attenuation *= attenuation;
                attenuation *= diffuse;

                directLighting +=
                    uLightColors[i] *
                    uLightIntensities[i] *
                    attenuation;
            }

            float4 scene = uSceneColor.Sample(
                uSampler,
                input.TexCoord);

            return float4(directLighting, scene.a);
        }
        """;

    private const string AmbientGlsl = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uSceneColor;
        uniform vec3 uAmbientColor;

        out vec4 FragColor;

        void main()
        {
            vec4 scene = texture(
                uSceneColor,
                vTexCoord);

            FragColor = vec4(
                uAmbientColor,
                scene.a);
        }
        """;

    private const string AmbientHlsl = """
        Texture2D<float4> uSceneColor : register(t0);
        SamplerState uSampler : register(s0);

        cbuffer LightingParameters : register(b0)
        {
            float3 uAmbientColor;
        };

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

            return float4(
                uAmbientColor,
                scene.a);
        }
        """;

    public static ShaderDescription CreatePointLightingDescription()
    {
        return new ShaderDescription(
            new ShaderVariant(
                ShaderLanguage.Glsl,
                FullscreenShaderSource.GlslVertex,
                PointLightGlsl),

            new ShaderVariant(
                ShaderLanguage.Hlsl,
                FullscreenShaderSource.HlslVertex,
                PointLightHlsl,
                "VSMain",
                "PSMain"));
    }

    public static ShaderDescription CreateAmbientLightingDescription()
    {
        return new ShaderDescription(
            new ShaderVariant(
                ShaderLanguage.Glsl,
                FullscreenShaderSource.GlslVertex,
                AmbientGlsl),

            new ShaderVariant(
                ShaderLanguage.Hlsl,
                FullscreenShaderSource.HlslVertex,
                AmbientHlsl,
                "VSMain",
                "PSMain"));
    }
}