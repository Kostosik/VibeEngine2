namespace Engine.Editor.Inspection;

public sealed class EditorPropertyProviderRegistry :
    IEditorPropertyProviderRegistry
{
    private readonly List<IEditorPropertyProvider> _providers =
        new();

    public IReadOnlyList<IEditorPropertyProvider> Providers =>
        _providers;

    public void Register(
        IEditorPropertyProvider provider)
    {
        ArgumentNullException.ThrowIfNull(
            provider);

        if (_providers.Contains(
                provider))
        {
            throw new InvalidOperationException(
                "Property provider is already registered.");
        }

        _providers.Add(
            provider);
    }

    public bool Unregister(
        IEditorPropertyProvider provider)
    {
        ArgumentNullException.ThrowIfNull(
            provider);

        return _providers.Remove(
            provider);
    }

    public bool TryGetProvider(
        Type type,
        out IEditorPropertyProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(
            type);

        provider = null;

        foreach (var candidate in _providers)
        {
            if (!candidate.CanInspect(type))
            {
                continue;
            }

            if (provider is null ||
                candidate.Priority > provider.Priority)
            {
                provider = candidate;
            }
        }

        return provider is not null;
    }
}