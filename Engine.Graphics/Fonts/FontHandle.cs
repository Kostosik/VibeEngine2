namespace Engine.Graphics.Fonts;

public readonly record struct FontHandle(uint Id)
{
    public bool IsValid =>
        Id != 0;

    public static FontHandle Invalid =>
        default;
}