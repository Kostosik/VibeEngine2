using Engine.Core.Math;

namespace Engine.Animation;

public sealed class Vector3LinearAnimationInterpolator
    : IAnimationInterpolator<Vector3>
{
    public Vector3 Interpolate(
        Vector3 from,
        Vector3 to,
        double amount)
    {
        if (!double.IsFinite(amount))
            throw new ArgumentOutOfRangeException(nameof(amount));

        var t = (float)amount;

        return new Vector3(
            from.X + (to.X - from.X) * t,
            from.Y + (to.Y - from.Y) * t,
            from.Z + (to.Z - from.Z) * t);
    }
}