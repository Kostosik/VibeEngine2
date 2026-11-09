namespace Engine.Graphics.Resources;

public readonly record struct TextureSamplerDescription(
    TextureFilter Minification,
    TextureFilter Magnification,
    TextureWrapMode WrapS,
    TextureWrapMode WrapT)
{
    public static TextureSamplerDescription Default => new(
        TextureFilter.Nearest,
        TextureFilter.Nearest,
        TextureWrapMode.ClampToEdge,
        TextureWrapMode.ClampToEdge);
}