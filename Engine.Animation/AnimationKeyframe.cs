namespace Engine.Animation;

public readonly record struct AnimationKeyframe<T>
{
    public AnimationKeyframe(
        double timeSeconds,
        T value)
    {
        if (!double.IsFinite(
                timeSeconds) ||
            timeSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds),
                "Keyframe time must be finite and non-negative.");
        }

        TimeSeconds =
            timeSeconds;

        Value =
            value;
    }

    public double TimeSeconds { get; }

    public T Value { get; }
}