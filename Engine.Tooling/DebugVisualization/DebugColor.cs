namespace Engine.Tooling.DebugVisualization;

public readonly record struct DebugColor(
    byte R,
    byte G,
    byte B,
    byte A = 255)
{
    public static DebugColor White =>
        new(255, 255, 255);

    public static DebugColor Red =>
        new(255, 0, 0);

    public static DebugColor Green =>
        new(0, 255, 0);

    public static DebugColor Blue =>
        new(0, 0, 255);

    public static DebugColor Yellow =>
        new(255, 255, 0);
}