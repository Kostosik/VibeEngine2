namespace Engine.Graphics.Materials;

public readonly record struct SpriteMaterialParameters
{
    public SpriteMaterialParameters()
        : this(1.0f, 1.0f)
    {
    }

    public SpriteMaterialParameters(
        float normalStrength,
        float emissionStrength)
    {
        if (!float.IsFinite(normalStrength) ||
            normalStrength < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalStrength));
        }

        if (!float.IsFinite(emissionStrength) ||
            emissionStrength < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emissionStrength));
        }

        NormalStrength = normalStrength;
        EmissionStrength = emissionStrength;
    }

    public float NormalStrength { get; init; }

    public float EmissionStrength { get; init; }

    public static SpriteMaterialParameters Default =>
        new(1.0f, 1.0f);
}