namespace Engine.Core.Time;

public readonly record struct Duration(
    TimeSpan Value)
{
    public static Duration Zero =>
        new(TimeSpan.Zero);

    public bool IsZero =>
        Value == TimeSpan.Zero;

    public bool IsNegative =>
        Value < TimeSpan.Zero;

    public static Duration FromSeconds(
        double seconds)
    {
        return new Duration(
            TimeSpan.FromSeconds(seconds));
    }

    public static Duration FromMilliseconds(
        double milliseconds)
    {
        return new Duration(
            TimeSpan.FromMilliseconds(milliseconds));
    }

    public double TotalSeconds =>
        Value.TotalSeconds;

    public double TotalMilliseconds =>
        Value.TotalMilliseconds;

    public static Duration operator +(
        Duration left,
        Duration right)
    {
        return new(
            left.Value + right.Value);
    }

    public static Duration operator -(
        Duration left,
        Duration right)
    {
        return new(
            left.Value - right.Value);
    }
}