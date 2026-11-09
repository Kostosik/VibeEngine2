
using System.Diagnostics.CodeAnalysis;
using Engine.Core.Time;

namespace Engine.AI.Memory;

/// <summary>
/// Short-term and persistent working memory owned by one AI agent.
/// </summary>
public sealed class AIMemory
{
    private readonly record struct Entry(
        object Value,
        TimeSpan? ExpiresAt);

    private readonly Dictionary<object, Entry> _entries =
        new(ReferenceEqualityComparer.Instance);

    private readonly List<object> _expiredKeys = new();

    public int Count => _entries.Count;

    /// <summary>
    /// Stores a value. A null lifetime means the value does not expire.
    /// A zero lifetime means it expires at the supplied time.
    /// </summary>
    public void Set<T>(
        AIMemoryKey<T> key,
        T value,
        TimeSpan now,
        Duration? timeToLive = null)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        TimeSpan? expiresAt = null;

        if (timeToLive.HasValue)
        {
            Duration lifetime = timeToLive.Value;

            if (lifetime.IsNegative)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeToLive));
            }

            expiresAt = now + lifetime.Value;
        }

        _entries[key] = new Entry(
            value,
            expiresAt);
    }

    public bool TryGet<T>(
        AIMemoryKey<T> key,
        TimeSpan now,
        [MaybeNullWhen(false)] out T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_entries.TryGetValue(key, out var entry))
        {
            value = default!;
            return false;
        }

        if (entry.ExpiresAt is TimeSpan expiresAt &&
            now >= expiresAt)
        {
            _entries.Remove(key);

            value = default!;
            return false;
        }

        value = (T)entry.Value;
        return true;
    }

    public bool Remove<T>(AIMemoryKey<T> key)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(key);

        return _entries.Remove(key);
    }

    /// <summary>
    /// Removes all entries whose lifetime has expired.
    /// </summary>
    public int PruneExpired(TimeSpan now)
    {
        _expiredKeys.Clear();

        foreach (var pair in _entries)
        {
            if (pair.Value.ExpiresAt is not TimeSpan expiresAt)
                continue;

            if (now >= expiresAt)
            {
                _expiredKeys.Add(pair.Key);
            }
        }

        foreach (var key in _expiredKeys)
        {
            _entries.Remove(key);
        }

        int removed = _expiredKeys.Count;
        _expiredKeys.Clear();

        return removed;
    }

    public T GetOrCreate<T>(
    AIMemoryKey<T> key,
    Func<T> factory)
    where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (_entries.TryGetValue(key, out var existing))
        {
            if (existing.ExpiresAt is not null)
            {
                throw new InvalidOperationException(
                    "GetOrCreate cannot be used with an expiring memory entry.");
            }

            if (existing.Value is T typed)
                return typed;

            throw new InvalidOperationException(
                "AI memory key is already used by another type.");
        }

        var value = factory();

        ArgumentNullException.ThrowIfNull(value);

        _entries.Add(
            key,
            new Entry(value, null));

        return value;
    }

    public void Clear()
    {
        _entries.Clear();
        _expiredKeys.Clear();
    }
}
