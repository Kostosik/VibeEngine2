
public sealed class Material3D
{
    public MaterialColor BaseColor { get; }

    /// <summary>
    /// Сила фонового освещения.
    /// </summary>
    public float AmbientStrength { get; }

    /// <summary>
    /// Сила направленного освещения.
    /// </summary>
    public float DiffuseStrength { get; }

    public static Material3D Default { get; } = new(
        new MaterialColor(0.25f, 0.65f, 1.0f),
        ambientStrength: 0.2f,
        diffuseStrength: 0.8f);

    public Material3D(
        MaterialColor baseColor,
        float ambientStrength = 0.2f,
        float diffuseStrength = 0.8f)
    {
        if (!IsValidColor(baseColor))
            throw new ArgumentOutOfRangeException(nameof(baseColor));

        ValidateStrength(ambientStrength, nameof(ambientStrength));
        ValidateStrength(diffuseStrength, nameof(diffuseStrength));

        BaseColor = baseColor;
        AmbientStrength = ambientStrength;
        DiffuseStrength = diffuseStrength;
    }

    private static bool IsValidColor(MaterialColor color)
    {
        return color.IsFinite &&
               IsInUnitRange(color.R) &&
               IsInUnitRange(color.G) &&
               IsInUnitRange(color.B) &&
               IsInUnitRange(color.A);
    }

    private static void ValidateStrength(float value, string name)
    {
        if (!IsInUnitRange(value))
            throw new ArgumentOutOfRangeException(name);
    }

    private static bool IsInUnitRange(float value)
    {
        return float.IsFinite(value) && value >= 0f && value <= 1f;
    }
}
