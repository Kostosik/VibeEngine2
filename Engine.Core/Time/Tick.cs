namespace Engine.Core.Time;

public readonly record struct Tick(ulong Value)
{
    public static Tick Zero => default;

    public static Tick operator +(Tick tick, ulong value)
    {
        return new(tick.Value + value);
    }

    public static Tick operator ++(Tick tick)
    {
        return new(tick.Value + 1);
    }
}