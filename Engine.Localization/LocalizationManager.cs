namespace Engine.Localization;

public sealed class LocalizationManager
{
    private readonly Dictionary<
        Locale,
        LocalizationTable> _tables = new();

    public Locale CurrentLocale { get; private set; }

    public Locale? FallbackLocale { get; private set; }

    public LocalizationManager(
        Locale currentLocale)
    {
        CurrentLocale =
            currentLocale;
    }

    public void SetLocale(
        Locale locale)
    {
        CurrentLocale =
            locale;
    }

    public void SetFallbackLocale(
        Locale? locale)
    {
        FallbackLocale =
            locale;
    }

    public void AddTable(
        LocalizationTable table)
    {
        ArgumentNullException.ThrowIfNull(
            table);

        _tables[table.Locale] =
            table;
    }

    public bool RemoveTable(
        Locale locale)
    {
        return _tables.Remove(
            locale);
    }

    public bool TryGetTable(
        Locale locale,
        out LocalizationTable? table)
    {
        return _tables.TryGetValue(
            locale,
            out table);
    }

    public bool TryGet(
    LocalizationKey key,
    out string? text)
    {
        if (TryGetFromLocale(
                CurrentLocale,
                key,
                out text))
        {
            return true;
        }

        if (CurrentLocale.Region is not null &&
            TryGetFromLocale(
                new Locale(
                    CurrentLocale.Language),
                key,
                out text))
        {
            return true;
        }

        if (FallbackLocale.HasValue &&
            TryGetFallback(
                FallbackLocale.Value,
                key,
                out text))
        {
            return true;
        }

        text =
            null;

        return false;
    }

    private bool TryGetFallback(
        Locale locale,
        LocalizationKey key,
        out string? text)
    {
        if (TryGetFromLocale(
                locale,
                key,
                out text))
        {
            return true;
        }

        if (locale.Region is not null)
        {
            return TryGetFromLocale(
                new Locale(
                    locale.Language),
                key,
                out text);
        }

        text =
            null;

        return false;
    }

    public string Get(
        LocalizationKey key)
    {
        return TryGet(
                key,
                out var text)
            ? text!
            : key.Value;
    }

    public string Format(
        LocalizationKey key,
        params object?[] arguments)
    {
        var text =
            Get(key);

        return arguments.Length == 0
            ? text
            : string.Format(
                text,
                arguments);
    }

    private bool TryGetFromLocale(
        Locale locale,
        LocalizationKey key,
        out string? text)
    {
        if (_tables.TryGetValue(
                locale,
                out var table) &&
            table.TryGet(
                key,
                out text))
        {
            return true;
        }

        text =
            null;

        return false;
    }
}