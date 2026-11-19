using Engine.Graphics.Materials;

namespace Engine.Graphics2D.Sprites;

public struct SpriteMaterialComponent
{
    public SpriteMaterialComponent()
        : this(
            string.Empty,
            string.Empty,
            SpriteMaterialParameters.Default)
    {
    }

    public SpriteMaterialComponent(
        string? normalTexturePath = null,
        string? emissionTexturePath = null,
        SpriteMaterialParameters? parameters = null)
    {
        NormalTexturePath = normalTexturePath ?? string.Empty;
        EmissionTexturePath = emissionTexturePath ?? string.Empty;

        Parameters = parameters ??
                     SpriteMaterialParameters.Default;
    }

    public string NormalTexturePath { get; set; }

    public string EmissionTexturePath { get; set; }

    public SpriteMaterialParameters Parameters { get; set; }

    public bool HasNormal =>
        !string.IsNullOrWhiteSpace(NormalTexturePath);

    public bool HasEmission =>
        !string.IsNullOrWhiteSpace(EmissionTexturePath);
}