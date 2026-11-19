using Engine.Graphics.Resources;

namespace Engine.Graphics.Materials;

public readonly record struct SpriteMaterial
{
    public SpriteMaterial(
        TextureHandle albedo,
        TextureHandle normal = default,
        TextureHandle emission = default)
        : this(
            albedo,
            normal,
            emission,
            SpriteMaterialParameters.Default)
    {
    }

    public SpriteMaterial(
        TextureHandle albedo,
        TextureHandle normal,
        TextureHandle emission,
        SpriteMaterialParameters parameters)
    {
        Albedo = albedo;
        Normal = normal;
        Emission = emission;
        Parameters = parameters;
    }

    public TextureHandle Albedo { get; init; }

    public TextureHandle Normal { get; init; }

    public TextureHandle Emission { get; init; }

    public SpriteMaterialParameters Parameters { get; init; }

    public bool HasNormal => Normal.IsValid;

    public bool HasEmission => Emission.IsValid;

    public static SpriteMaterial AlbedoOnly(
        TextureHandle albedo)
    {
        return new SpriteMaterial(albedo);
    }
}