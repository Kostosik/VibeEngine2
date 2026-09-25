namespace Engine.Core.Assets;

public readonly record struct AssetPath
{
    public AssetPath(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        Value = Normalize(value);
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