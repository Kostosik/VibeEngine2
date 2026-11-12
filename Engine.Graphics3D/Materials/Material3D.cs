
using Engine.Core.Math;

public readonly record struct Material3D
{
    public Vector3 BaseColor { get; }
    public float AmbientStrength { get; }
    public float DiffuseStrength { get; }

    public static Material3D Default { get; } = new(
        new Vector3(0.25f, 0.65f, 1.0f),
        ambientStrength: 0.2f,
        diffuseStrength: 0.8f);

    public Material3D(
        Vector3 baseColor,
        float ambientStrength = 0.2f,
        float diffuseStrength = 0.8f)
    {
        ValidateColor(baseColor);

        ValidateUnitValue(
            ambientStrength,
            nameof(ambientStrength));

        ValidateUnitValue(
            diffuseStrength,
            nameof(diffuseStrength));

        BaseColor = baseColor;
        AmbientStrength = ambientStrength;
        DiffuseStrength = diffuseStrength;
    }

    private static void ValidateColor(Vector3 color)
    {
        if (!float.IsFinite(color.X) ||
            !float.IsFinite(color.Y) ||
            !float.IsFinite(color.Z) ||
            color.X < 0f || color.X > 1f ||
            color.Y < 0f || color.Y > 1f ||
            color.Z < 0f || color.Z > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(color));
        }
    }

    private static void ValidateUnitValue(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(name);
    }
}
