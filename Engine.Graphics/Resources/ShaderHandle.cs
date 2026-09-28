namespace Engine.Graphics.Resources;

public readonly record struct ShaderHandle(
    uint Value)
{
    public bool IsValid =>
        Value != 0;

    public static ShaderHandle Invalid =>
        default;
}