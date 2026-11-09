namespace Engine.Graphics.Resources;

public readonly record struct TextureDescription(
    int Width,
    int Height,
    TextureFormat Format)
{
    public TextureSamplerDescription? Sampler { get; init; }

    public TextureSamplerDescription EffectiveSampler =>
        Sampler ?? TextureSamplerDescription.Default;
}