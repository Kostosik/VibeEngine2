namespace Engine.Localization;

public sealed class LocalizationTable
{
    private readonly Dictionary<
        LocalizationKey,
        string> _entries = new();

    public Locale Locale { get; }

    public int Count =>
        _entries.Count;

    public LocalizationTable(
        Locale locale)
    {
        Locale =
            locale;
    }

    public void Set(
        LocalizationKey key,
        string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            text);

        _entries[key] =
            text;
    }

    public bool Remove(
        LocalizationKey key)
    {
        return _entries.Remove(
            key);
    }

    public bool Contains(
        LocalizationKey key)
    {
        return _entries.ContainsKey(
            key);
    }

    public bool TryGet(
        LocalizationKey key,
        out string? text)
    {
        return _entries.TryGetValue(
            key,
            out text);
    }

    public void Clear()
    {
        _entries.Clear();
    }
}