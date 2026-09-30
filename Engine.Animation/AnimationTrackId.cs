namespace Engine.Animation;

public readonly record struct AnimationTrackId
{
    public AnimationTrackId(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        Value =
            value.Trim();
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}