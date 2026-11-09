namespace Engine.Graphics.Resources;

public readonly record struct TextureSamplerDescription(
    TextureFilter Minification,
    TextureFilter Magnification,
    TextureWrapMode WrapS,
    TextureWrapMode WrapT,
    float Anisotropy = 1.0f)
{
    public static TextureSamplerDescription Default => new(
        TextureFilter.Nearest,
        TextureFilter.Nearest,
        TextureWrapMode.ClampToEdge,
        TextureWrapMode.ClampToEdge,
        1.0f);
}