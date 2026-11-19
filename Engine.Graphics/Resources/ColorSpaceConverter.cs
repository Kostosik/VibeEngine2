namespace Engine.Graphics.Resources;

public static class ColorSpaceConverter
{
    public static float SrgbToLinear(float value)
    {
        return value <= 0.04045f
            ? value / 12.92f
            : MathF.Pow(
                (value + 0.055f) / 1.055f,
                2.4f);
    }

    public static float LinearToSrgb(float value)
    {
        return value <= 0.0031308f
            ? value * 12.92f
            : 1.055f * MathF.Pow(
                value,
                1.0f / 2.4f) - 0.055f;
    }
}