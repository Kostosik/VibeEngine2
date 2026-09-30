namespace Engine.Animation;

public interface IAnimationInterpolator<T>
{
    T Interpolate(
        T from,
        T to,
        double amount);
}