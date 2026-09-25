namespace Engine.Graphics.Resources;

public readonly record struct TextureDescription(
    int Width,
    int Height,
    TextureFormat Format);