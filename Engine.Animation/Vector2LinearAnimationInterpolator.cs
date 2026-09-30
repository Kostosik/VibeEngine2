using Engine.Core.Math;

namespace Engine.Animation;

public sealed class Vector2LinearAnimationInterpolator
    : IAnimationInterpolator<Vector2>
{
    public Vector2 Interpolate(
        Vector2 from,
        Vector2 to,
        double amount)
    {
        if (!double.IsFinite(amount))
            throw new ArgumentOutOfRangeException(nameof(amount));

        var t = (float)amount;

        return new Vector2(
            from.X + (to.X - from.X) * t,
            from.Y + (to.Y - from.Y) * t);
    }
}