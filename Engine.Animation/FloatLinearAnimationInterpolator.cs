namespace Engine.Animation;

public sealed class FloatLinearAnimationInterpolator :
    IAnimationInterpolator<float>
{
    public float Interpolate(
        float from,
        float to,
        double amount)
    {
        if (!double.IsFinite(
                amount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount));
        }

        return from +
               (to - from) *
               (float)amount;
    }
}