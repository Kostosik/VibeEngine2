namespace Engine.Localization;

public readonly record struct Locale
{
    public Locale(
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        var normalizedCode =
            code.Trim()
                .Replace(
                    '_',
                    '-');

        var parts =
            normalizedCode.Split(
                '-',
                StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            throw new ArgumentException(
                "Locale code is invalid.",
                nameof(code));
        }

        if (parts.Length > 2)
        {
            throw new ArgumentException(
                "Locale code cannot contain more than language and region.",
                nameof(code));
        }

        var language =
            parts[0].ToLowerInvariant();

        if (language.Length is < 2 or > 8)
        {
            throw new ArgumentException(
                "Locale language code is invalid.",
                nameof(code));
        }

        Language =
            language;

        Region =
            parts.Length > 1
                ? parts[1].ToUpperInvariant()
                : null;
    }

    public string Language { get; }

    public string? Region { get; }

    public string Code =>
        Region is null
            ? Language
            : $"{Language}-{Region}";

    public override string ToString()
    {
        return Code;
    }
}