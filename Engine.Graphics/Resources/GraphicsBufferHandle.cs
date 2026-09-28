namespace Engine.Graphics.Resources;

public readonly record struct GraphicsBufferHandle(
    uint Value)
{
    public bool IsValid =>
        Value != 0;

    public static GraphicsBufferHandle Invalid =>
        default;
}