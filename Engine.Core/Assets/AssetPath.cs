namespace Engine.Core.Assets;

public readonly record struct AssetPath
{
    public AssetPath(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        var normalized = Normalize(value);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalized,
            nameof(value));

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    private static string Normalize(
        string value)
    {
        return value
            .Replace('\\', '/')
            .Trim('/');
    }
}