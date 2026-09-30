namespace Engine.Animation;

public sealed class StepAnimationInterpolator<T> :
    IAnimationInterpolator<T>
{
    public T Interpolate(
        T from,
        T to,
        double amount)
    {
        if (!double.IsFinite(
                amount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount));
        }

        return from;
    }
}