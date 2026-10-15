namespace Engine.Scripting;

public sealed class ScriptRegistry
{
    private readonly Dictionary<
        string,
        Func<IEntityScript>> _factories =
            new(StringComparer.Ordinal);

    public int Count =>
        _factories.Count;

    public IReadOnlyList<string> RegisteredIds =>
        _factories.Keys
            .OrderBy(
                id => id,
                StringComparer.Ordinal)
            .ToArray();

    public void Register<TScript>(
        string id)
        where TScript : class, IEntityScript, new()
    {
        Register(
            id,
            static () => new TScript());
    }

    public void Register(
        string id,
        Func<IEntityScript> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(factory);

        if (!_factories.TryAdd(
                id,
                factory))
        {
            throw new InvalidOperationException(
                $"Script '{id}' is already registered.");
        }
    }

    public bool Contains(
        string id)
    {
        return _factories.ContainsKey(id);
    }

    public IEntityScript Create(
        string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_factories.TryGetValue(
                id,
                out var factory))
        {
            throw new KeyNotFoundException(
                $"Script '{id}' is not registered.");
        }

        return factory()
            ?? throw new InvalidOperationException(
                $"Factory for script '{id}' returned null.");
    }
}