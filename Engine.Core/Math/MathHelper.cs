namespace Engine.Core.Math;

public static class MathHelper
{
    public const float Pi = MathF.PI;
    public const float TwoPi = MathF.PI * 2.0f;

    public static float DegreesToRadians(float degrees)
    {
        return degrees * (Pi / 180.0f);
    }

    public static float RadiansToDegrees(float radians)
    {
        return radians * (180.0f / Pi);
    }

    /// <summary>
    /// Wraps an angle into the range [-Pi, Pi).
    /// </summary>
    public static float WrapAngle(float radians)
    {
        if (!float.IsFinite(radians))
        {
            throw new ArgumentOutOfRangeException(nameof(radians));
        }

        var wrapped = radians % TwoPi;

        if (wrapped < -Pi)
        {
            wrapped += TwoPi;
        }
        else if (wrapped >= Pi)
        {
            wrapped -= TwoPi;
        }

        return wrapped;
    }

    /// <summary>
    /// Linear interpolation. Amount is not clamped.
    /// </summary>
    public static float Lerp(
        float start,
        float end,
        float amount)
    {
        return start + (end - start) * amount;
    }

    /// <summary>
    /// Smooth interpolation. Amount is clamped to [0, 1].
    /// </summary>
    public static float SmoothStep(
        float start,
        float end,
        float amount)
    {
        amount = System.Math.Clamp(amount, 0.0f, 1.0f);

        var smoothAmount =
            amount * amount * (3.0f - 2.0f * amount);

        return Lerp(start, end, smoothAmount);
    }
}