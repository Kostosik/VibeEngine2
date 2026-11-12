using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public sealed class VignetteEffect : IPostProcessEffect
{
    public const string EffectName = "Vignette";

    private float _intensity = 0.65f;
    private float _radius = 0.35f;
    private float _softness = 0.25f;

    public string Name => EffectName;

    public bool Enabled { get; set; } = true;

    public ShaderHandle Shader { get; }

    /// <summary>
    /// Darkness strength. Range: 0..1.
    /// </summary>
    public float Intensity
    {
        get => _intensity;
        set => _intensity = ValidateRange(
            value,
            nameof(value));
    }

    /// <summary>
    /// Distance from the center where edge darkening begins.
    /// Range: 0..1.
    /// </summary>
    public float Radius
    {
        get => _radius;
        set => _radius = ValidateRange(
            value,
            nameof(value));
    }

    /// <summary>
    /// Smooth transition width. Must be greater than zero.
    /// Range: (0..1].
    /// </summary>
    public float Softness
    {
        get => _softness;
        set
        {
            if (!float.IsFinite(value) ||
                value <= 0.0f ||
                value > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Softness must be greater than zero and not exceed one.");
            }

            _softness = value;
        }
    }

    public VignetteEffect(ShaderHandle shader)
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

        writer.SetFloat("uIntensity", _intensity);
        writer.SetFloat("uRadius", _radius);
        writer.SetFloat("uSoftness", _softness);
    }


    public static ShaderDescription CreateShaderDescription()
    {
        return FullscreenShaderSource.CreateDescription(
            FragmentShaderSource,
            HlslFragmentShaderSource);
    }


    private static float ValidateRange(
        float value,
        string parameterName)
    {
        if (!float.IsFinite(value) ||
            value < 0.0f ||
            value > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Value must be between zero and one.");
        }

        return value;
    }

    public const string FragmentShaderSource = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uSource;
        uniform vec2 uResolution;

        uniform float uIntensity;
        uniform float uRadius;
        uniform float uSoftness;

        out vec4 FragColor;

        void main()
        {
            vec4 color = texture(uSource, vTexCoord);

            // Переводим UV-координаты в координаты,
            // учитывающие реальные пропорции экрана.
            vec2 centered =
                (vTexCoord - vec2(0.5)) * uResolution;

            float shortestSide =
                min(uResolution.x, uResolution.y);

            float distanceFromCenter =
                length(centered) / shortestSide;

            // Плавное нарастание затемнения к краям.
            float vignette = smoothstep(
                uRadius,
                uRadius + uSoftness,
                distanceFromCenter);

            float darkness =
                vignette * uIntensity;

            FragColor = vec4(
                color.rgb * (1.0 - darkness),
                color.a);
        }
        """;


    public const string HlslFragmentShaderSource = """
    Texture2D uSource : register(t0);
    SamplerState uSourceSampler : register(s0);

    cbuffer EffectParameters : register(b0)
    {
        float2 uResolution;
        float uIntensity;
        float uRadius;
        float uSoftness;
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

        float2 centered =
            (input.TexCoord - float2(0.5f, 0.5f))
            * uResolution;

        float shortestSide = min(
            uResolution.x,
            uResolution.y);

        float distanceFromCenter =
            length(centered) / shortestSide;

        float vignette = smoothstep(
            uRadius,
            uRadius + uSoftness,
            distanceFromCenter);

        float darkness = vignette * uIntensity;

        return float4(
            color.rgb * (1.0f - darkness),
            color.a);
    }
    """;

}