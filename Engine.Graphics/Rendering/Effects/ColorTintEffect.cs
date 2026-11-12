using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public sealed class ColorTintEffect : IPostProcessEffect
{
    public const string EffectName = "ColorTint";

    private Vector3 _tint = Vector3.One;
    private float _intensity = 1.0f;

    public string Name => EffectName;

    public bool Enabled { get; set; } = true;

    public ShaderHandle Shader { get; }

    /// <summary>
    /// RGB multiplier applied to the source image.
    /// Each component must be finite and non-negative.
    /// </summary>
    public Vector3 Tint
    {
        get => _tint;
        set
        {
            if (!float.IsFinite(value.X) ||
                !float.IsFinite(value.Y) ||
                !float.IsFinite(value.Z) ||
                value.X < 0.0f ||
                value.Y < 0.0f ||
                value.Z < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Tint components must be finite and non-negative.");
            }

            _tint = value;
        }
    }

    /// <summary>
    /// Blending strength between the original image and tinted image.
    /// Range: 0..1.
    /// </summary>
    public float Intensity
    {
        get => _intensity;
        set
        {
            if (!float.IsFinite(value) ||
                value < 0.0f ||
                value > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Intensity must be between 0 and 1.");
            }

            _intensity = value;
        }
    }

    public ColorTintEffect(ShaderHandle shader)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentException(
                "A valid shader handle is required.",
                nameof(shader));
        }

        Shader = shader;
    }

    public void ApplyParameters(IPostProcessParameterWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.SetVector3("uTint", _tint);
        writer.SetFloat("uIntensity", _intensity);
    }


    public static ShaderDescription CreateShaderDescription()
    {
        return FullscreenShaderSource.CreateDescription(
            FragmentShaderSource,
            HlslFragmentShaderSource);
    }


    public const string FragmentShaderSource = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uSource;
        uniform vec3 uTint;
        uniform float uIntensity;

        out vec4 FragColor;

        void main()
        {
            vec4 color = texture(uSource, vTexCoord);

            vec3 tinted = color.rgb * uTint;

            vec3 result = mix(
                color.rgb,
                tinted,
                uIntensity);

            FragColor = vec4(result, color.a);
        }
        """;


    public const string HlslFragmentShaderSource = """
    Texture2D uSource : register(t0);
    SamplerState uSourceSampler : register(s0);

    cbuffer EffectParameters : register(b0)
    {
        float3 uTint;
        float uIntensity;
    };

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

        float3 tinted = color.rgb * uTint;

        float3 result = lerp(
            color.rgb,
            tinted,
            uIntensity);

        return float4(result, color.a);
    }
    """;

}