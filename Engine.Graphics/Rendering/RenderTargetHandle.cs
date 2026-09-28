namespace Engine.Graphics.Resources;

public readonly record struct RenderTargetHandle(
    uint Value)
{
    public bool IsValid =>
        Value != 0;

    public static RenderTargetHandle Invalid =>
        default;
}