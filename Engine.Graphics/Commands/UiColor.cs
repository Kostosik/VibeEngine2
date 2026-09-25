namespace Engine.Graphics.Commands;

public readonly record struct UiColor(
    byte R,
    byte G,
    byte B,
    byte A = 255)
{
    public static UiColor White =>
        new(255, 255, 255);

    public static UiColor Black =>
        new(0, 0, 0);

    public static UiColor Transparent =>
        new(0, 0, 0, 0);
}