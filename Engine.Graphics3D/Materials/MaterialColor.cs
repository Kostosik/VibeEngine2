
public readonly record struct MaterialColor(
    float R,
    float G,
    float B,
    float A = 1.0f)
{
    public bool IsFinite =>
        float.IsFinite(R) &&
        float.IsFinite(G) &&
        float.IsFinite(B) &&
        float.IsFinite(A);

    public static MaterialColor White => new(1f, 1f, 1f, 1f);
}
