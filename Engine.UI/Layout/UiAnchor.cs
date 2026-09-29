namespace Engine.UI.Layout;

public readonly record struct UiAnchor
{
    public UiAnchor(
        float x,
        float y)
    {
        Validate(x);
        Validate(y);

        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }

    public static UiAnchor TopLeft =>
        new(0.0f, 0.0f);

    public static UiAnchor TopCenter =>
        new(0.5f, 0.0f);

    public static UiAnchor TopRight =>
        new(1.0f, 0.0f);

    public static UiAnchor CenterLeft =>
        new(0.0f, 0.5f);

    public static UiAnchor Center =>
        new(0.5f, 0.5f);

    public static UiAnchor CenterRight =>
        new(1.0f, 0.5f);

    public static UiAnchor BottomLeft =>
        new(0.0f, 1.0f);

    public static UiAnchor BottomCenter =>
        new(0.5f, 1.0f);

    public static UiAnchor BottomRight =>
        new(1.0f, 1.0f);

    private static void Validate(
        float value)
    {
        if (!float.IsFinite(value) ||
            value < 0.0f ||
            value > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Anchor values must be finite and between 0 and 1.");
        }
    }
}