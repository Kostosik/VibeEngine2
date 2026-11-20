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

    /// <summary>
    /// Optional tangent-space normal map.
    /// Texture format: RGBA8 UNorm.
    /// Color space: Linear.
    /// RGB encodes tangent-space normal as [0, 1].
    /// A flat normal is represented by RGB = (0.5, 0.5, 1.0).
    /// +X points right, +Y points up, +Z points out of the sprite.
    /// Alpha is ignored.
    /// </summary>
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