using System.Diagnostics.CodeAnalysis;

namespace Engine.AI.Core;

public sealed class AIAgentState
{
    private readonly Dictionary<object, object> _values =
        new(ReferenceEqualityComparer.Instance);

    public T GetOrCreate<T>(
        object key,
        Func<T> factory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (_values.TryGetValue(key, out var existing))
        {
            if (existing is T typed)
                return typed;

            throw new InvalidOperationException(
                "AI state key is already used by another type.");
        }

        var value = factory();

        ArgumentNullException.ThrowIfNull(value);

        _values.Add(key, value);

        return value;
    }

    public bool TryGet<T>(
        object key,
        [NotNullWhen(true)] out T? value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_values.TryGetValue(key, out var existing) &&
            existing is T typed)
        {
            value = typed;
            return true;
        }

        value = null;
        return false;
    }

    public bool Remove(object key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _values.Remove(key);
    }

    public void Clear()
    {
        _values.Clear();
    }
}