namespace Engine.Localization;

public readonly record struct LocalizationKey
{
    public LocalizationKey(
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

    public static implicit operator LocalizationKey(
        string value)
    {
        return new LocalizationKey(
            value);
    }
}