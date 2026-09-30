namespace Engine.Animation;

public sealed class DelegateAnimationInterpolator<T> :
    IAnimationInterpolator<T>
{
    private readonly Func<T, T, double, T> _interpolate;

    public DelegateAnimationInterpolator(
        Func<T, T, double, T> interpolate)
    {
        ArgumentNullException.ThrowIfNull(
            interpolate);

        _interpolate =
            interpolate;
    }

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

        return _interpolate(
            from,
            to,
            amount);
    }
}