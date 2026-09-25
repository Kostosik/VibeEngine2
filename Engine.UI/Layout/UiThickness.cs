namespace Engine.UI.Layout;

public readonly record struct UiThickness(
    float Left,
    float Top,
    float Right,
    float Bottom)
{
    public UiThickness(
        float uniform)
        : this(
            uniform,
            uniform,
            uniform,
            uniform)
    {
    }

    public static UiThickness Zero =>
        new(0.0f);
}